// SPDX-FileCopyrightText: yarb00 <https://www.yarb00.dev>
// SPDX-License-Identifier: MPL-2.0

// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TsslInterpreter;

internal readonly record struct LanguageInstruction(string CommandName, LanguageString[] CommandArguments, int SourceLine)
{
	public bool IsUserDefinedCommand => CommandName.StartsWith("my ");

	public string? UserDefinedCommandName => IsUserDefinedCommand ? CommandName["my ".Length..] : null;

	public override readonly string ToString()
	{
		string result = $"{CommandName}>";

		foreach (LanguageString argument in CommandArguments) result += $" {argument};";

		return result;
	}
}

internal readonly record struct Script(string LanguageVersion, LanguageInstruction[] Instructions, FrozenDictionary<string, int> InstructionIndexByLabel, int TotalSourceLines)
{
	private enum ArgumentParsingState
	{
		JustStarted,
		PlainText,
		EscapeSequence,
		Interpolation,
	}

	public static Script ParseFromLines(string[] sourceLines)
	{
		const string header = "!TooSimpleScriptingLanguage"; // `!TooSimpleScriptingLanguage VERSION`

		sourceLines = sourceLines
			.Select(static line => line.LanguageTrim())
			.Select(static line => line.StartsWith('#') ? string.Empty : line)
			.ToArray();

		(string LanguageVersion, int DeclarationLine)? versionDeclaration = null;

		List<LanguageInstruction> instructions = [with(capacity: sourceLines.Length)];

		List<string> labelsToBeAttached = [with(capacity: sourceLines.Length)];

		Dictionary<string, int> instructionIndexByLabel = new(capacity: sourceLines.Length, StringComparer.OrdinalIgnoreCase);

		Dictionary<string, int> sourceLineByLabel = new(capacity: sourceLines.Length, StringComparer.OrdinalIgnoreCase);

		foreach ((int index, string line) in sourceLines.Index())
		{
			if (line.IsEmpty) continue;

			if (line.StartsWith(header, StringComparison.OrdinalIgnoreCase))
			{
				if (versionDeclaration is not null) throw InvalidScriptException.LanguageVersionAlreadySet(index + 1, versionDeclaration.Value.DeclarationLine);

				string version = line[header.Length..].LanguageTrimStart();

				if (version.IsEmpty) throw InvalidScriptException.InvalidSyntax(index + 1, $"A valid language version declaration must be in format `!TooSimpleScriptingLanguage VERSION`, but the script declares: `{line}`.");

				versionDeclaration = (version, index + 1);

				continue;
			}

			if (versionDeclaration is null)
			{
				if (sourceLines.Any(static line => line.StartsWith(header, StringComparison.OrdinalIgnoreCase)))
					throw InvalidScriptException.LanguageVersionSetTooLate(index + 1);

				throw InvalidScriptException.LanguageVersionNotSet();
			}

			if (line.StartsWith('@', StringComparison.OrdinalIgnoreCase))
			{
				string label = line[1..].LanguageTrimStart();

				if (!label.IsValidLanguageIdentifier) throw InvalidScriptException.InvalidIdentifierName(index + 1, label);

				if (sourceLineByLabel.TryGetValue(label, out int previousLine)) throw InvalidScriptException.LabelAlreadyDefined(index + 1, label, previousLine);

				sourceLineByLabel.Add(label, index + 1);
				labelsToBeAttached.Add(label);

				continue;
			}

			int nameSeparatorIndex = line.IndexOf('>');

			if (nameSeparatorIndex == -1) throw InvalidScriptException.InvalidSyntax(index + 1, "Expected the line to be an instruction, but no `>` separator was found.");

			string name = line[..nameSeparatorIndex].LanguageTrimEnd(), rest = line[(nameSeparatorIndex + 1)..];

			if (!name.IsValidLanguageIdentifier) throw InvalidScriptException.InvalidIdentifierName(index + 1, name);

			LanguageString[] arguments = ParseArguments(index, rest);

			instructions.Add(new(name, arguments, index + 1));

			foreach (string label in labelsToBeAttached) instructionIndexByLabel.Add(label, instructions.Count - 1);

			labelsToBeAttached.Clear();
		}

		foreach (string label in labelsToBeAttached) instructionIndexByLabel.Add(label, -1);

		if (versionDeclaration is null) throw InvalidScriptException.LanguageVersionNotSet();

		return new(versionDeclaration.Value.LanguageVersion, [.. instructions], instructionIndexByLabel.ToFrozenDictionary(), sourceLines.Length);
	}

	private static LanguageString[] ParseArguments(int lineIndex, string rest)
	{
		if (rest.IsEmpty) return [];

		List<LanguageString> arguments = [];

		List<LanguageStringAtom> argument = [with(capacity: rest.Length)];

		ArgumentParsingState state = ArgumentParsingState.JustStarted;

		Stack<List<LanguageStringAtom>> interpolationStack = [];

		bool interpolationIsInsideEscapeSequence = false;

		for (int i = 0; i < rest.Length; i++)
		{
			char current = rest[i];

			if (state == ArgumentParsingState.JustStarted)
			{
				if (current != ' ')
					throw InvalidScriptException.InvalidSyntax(lineIndex + 1, "In instructions, a single space is required after `>` and `;`, unless they are the last character on the line.");

				state = ArgumentParsingState.PlainText;
				continue;
			}

			if (state == ArgumentParsingState.PlainText)
			{
				if (current == '\\') state = ArgumentParsingState.EscapeSequence;
				else if (current == ';')
				{
					LanguageString newArgument = [.. argument];
					arguments.Add(newArgument);
					argument.Clear();

					state = ArgumentParsingState.JustStarted;
				}
				else argument.Add(current);

				continue;
			}

			if (state == ArgumentParsingState.EscapeSequence)
			{
				if (current is '\\' or ';')
				{
					argument.Add(current);
					state = ArgumentParsingState.PlainText;
				}
				else if (current == '(')
				{
					interpolationStack.Push([]);
					state = ArgumentParsingState.Interpolation;
				}
				else throw InvalidScriptException.UnknownEscapeSequence(lineIndex + 1, current.ToString());

				continue;
			}

			if (state == ArgumentParsingState.Interpolation)
			{
				if (interpolationIsInsideEscapeSequence)
				{
					if (current != '(')
						throw InvalidScriptException.InvalidStringInterpolation(lineIndex + 1);

					interpolationStack.Push([]);
					interpolationIsInsideEscapeSequence = false;
					continue;
				}

				if (current == '\\')
				{
					interpolationIsInsideEscapeSequence = true;
					continue;
				}

				if (current == ')')
				{
					List<LanguageStringAtom> currentAtoms = interpolationStack.Pop();
					LanguageString currentVariableName = [.. currentAtoms];
					LanguageStringInterpolation currentInterpolation = new(currentVariableName);

					if (interpolationStack.TryPeek(out List<LanguageStringAtom>? previousAtoms)) previousAtoms.Add(currentInterpolation);
					else
					{
						argument.Add(currentInterpolation);
						state = ArgumentParsingState.PlainText;
					}

					continue;
				}

				if (current == ' ' || current.ToString().IsValidLanguageIdentifier)
				{
					interpolationStack.Peek().Add(current);
					continue;
				}

				throw InvalidScriptException.InvalidStringInterpolation(lineIndex + 1);
			}

			throw new UnreachableException("Invalid argument parsing state.");
		}

		if (interpolationStack.Count != 0) throw InvalidScriptException.InterpolationNotClosed(lineIndex + 1);

		if (state != ArgumentParsingState.JustStarted) throw InvalidScriptException.LastArgumentNotClosed(lineIndex + 1);

		return [.. arguments];
	}
}
