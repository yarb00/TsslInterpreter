// SPDX-FileCopyrightText: yarb00 <https://www.yarb00.dev>
// SPDX-License-Identifier: MPL-2.0

// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace TsslInterpreter;

internal readonly record struct LanguageStringInterpolation(params LanguageString VariableName);

internal readonly union LanguageStringAtom(string, LanguageStringInterpolation)
{
	public static implicit operator LanguageStringAtom(char character) => character.ToString();

	public readonly string Calculate(int line, IReadOnlyDictionary<string, string> variables)
	{
		switch (this)
		{
			// default(LanguageStringAtom) should be an empty string
			case null: return string.Empty;

			case string @string: return @string;

			case LanguageStringInterpolation interpolation:
				// default(LanguageStringInterpolation) should be invalid
				string variableName = interpolation.VariableName.Calculate(line, variables);
				if (!variableName.IsValidLanguageIdentifier) throw InvalidScriptException.InvalidIdentifierName(line, variableName);
				if (!variables.TryGetValue(variableName.LanguageTrim(), out string? variableValue)) throw InvalidScriptException.UnknownVariableExpanded(line, variableName);
				return variableValue;
		}
	}
}

[CollectionBuilder(typeof(LanguageString), nameof(Create))]
internal readonly struct LanguageString(params ReadOnlySpan<LanguageStringAtom> atoms) : IEnumerable<LanguageStringAtom>
{
	private readonly ImmutableArray<LanguageStringAtom> atoms = atoms.ToImmutableArray();

	public static implicit operator LanguageString(LanguageStringAtom atom) => new(atom);

	public static implicit operator LanguageString(LanguageStringInterpolation interpolation) => new(interpolation);

	public static implicit operator LanguageString(string @string) => new(@string);

	public static implicit operator LanguageString(char character) => new(character);

	public static LanguageString Create(params ReadOnlySpan<LanguageStringAtom> atoms) => new(atoms);

	public readonly string Calculate(int line, IReadOnlyDictionary<string, string> variables)
	{
		// default(LanguageString) and [] should be an empty string
		if (atoms.IsDefaultOrEmpty) return string.Empty;

		string result = string.Empty;

		foreach (LanguageStringAtom atom in atoms) result += atom.Calculate(line, variables);

		return result;
	}

	public override readonly string ToString()
	{
		// default(LanguageString) and [] should be an empty string
		if (atoms.IsDefaultOrEmpty) return string.Empty;

		string result = string.Empty;

		foreach (LanguageStringAtom atom in atoms) result += atom switch
		{
			null => string.Empty,
			string @string => @string.Replace(@"\", @"\\").Replace(@";", @"\;"),
			LanguageStringInterpolation interpolation => $"\\({interpolation.VariableName})",
		};

		return result;
	}

	IEnumerator<LanguageStringAtom> IEnumerable<LanguageStringAtom>.GetEnumerator() => ((IEnumerable<LanguageStringAtom>)atoms).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)atoms).GetEnumerator();
}
