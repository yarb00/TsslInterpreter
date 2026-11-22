// SPDX-FileCopyrightText: yarb00 <https://www.yarb00.dev>
// SPDX-License-Identifier: MPL-2.0

// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace TsslInterpreter;

internal enum ScriptError
{
	Unknown = 0,

	// Invalid syntax
	InvalidSyntax,
	InvalidIdentifierName,
	LastArgumentNotClosed,

	// Invalid blocks
	NestedCommandDefined,
	CommandFallenThrough,
	BlockNotClosed,

	// Language version declaration
	LanguageVersionNotSet,
	LanguageVersionSetTooLate,
	LanguageVersionAlreadySet,

	// Commands
	UnknownBuiltInCommandCalled,
	UnknownUserDefinedCommandCalled,
	UnknownConditionCalled,
	UnknownSignatureCalled,
	InvalidArgumentsPassed,
	CommandAlreadyDefined,

	// Variables
	UnknownVariableExpanded,
	ProtectedVariableSet,

	// Labels
	UnknownLabelJumpedTo,
	LabelAlreadyDefined,

	// Strings
	UnknownEscapeSequence,
	InvalidStringInterpolation,
	InterpolationNotClosed,
}

internal sealed class InvalidScriptException(int line, ScriptError reason = ScriptError.Unknown, string? details = null) : Exception(messageByError[reason])
{
	public int Line { get; private init; } = line;

	public ScriptError Reason { get; private init; } = reason;

	public string? Details { get; private init; } = details;

	private static readonly FrozenDictionary<ScriptError, string> messageByError = new Dictionary<ScriptError, string>()
	{
		[ScriptError.Unknown] = "An error occurred.",

		// Invalid syntax
		[ScriptError.InvalidSyntax] = "Invalid syntax.",
		[ScriptError.InvalidIdentifierName] = "The specified identifier name is invalid: only Latin letters (case-insensitive), digits (0-9), underscores and spaces are allowed.",
		[ScriptError.LastArgumentNotClosed] = "The last argument of the instruction is not terminated with a semicolon. All arguments must be terminated with a semicolon.",

		// Invalid blocks
		[ScriptError.NestedCommandDefined] = "Nested command definitions are not allowed.",
		[ScriptError.CommandFallenThrough] = "The command definition has fallen through. All user-defined commands must call one of the following before ending (`end command>`): `stop>`, `stop> MESSAGE;`, `jump> LABEL NAME;` or another user-defined command.",
		[ScriptError.BlockNotClosed] = "The block is not closed. `command> NAME;`, `if ...> ...;` and `while ...> ...;` must have a matching `end command>`, `end if>` or `end while>` instruction, respectively.",

		// Language version declaration
		[ScriptError.LanguageVersionNotSet] = "A language version declaration (`!TooSimpleScriptingLanguage VERSION`) must be present in any script as a first non-comment line.",
		[ScriptError.LanguageVersionSetTooLate] = "Only comments (lines starting with `#`) can be present before the language version declaration (`!TooSimpleScriptingLanguage VERSION`).",
		[ScriptError.LanguageVersionAlreadySet] = "Language version is already set. Only one language version declaration (`!TooSimpleScriptingLanguage VERSION`) can be present in the script.",

		// Commands
		[ScriptError.UnknownBuiltInCommandCalled] = "The specified command does not exist.",
		[ScriptError.UnknownUserDefinedCommandCalled] = "The specified command does not exist.",
		[ScriptError.UnknownConditionCalled] = "The specified condition does not exist.",
		[ScriptError.UnknownSignatureCalled] = "No signatures of the specified command match the passed argument count.",
		[ScriptError.InvalidArgumentsPassed] = "The passed arguments are invalid for the called command.",
		[ScriptError.CommandAlreadyDefined] = "A user-defined command with the same name was already created.",

		// Variables
		[ScriptError.UnknownVariableExpanded] = "The specified variable does not exist.",
		[ScriptError.ProtectedVariableSet] = "Variable names starting with an underscore are reserved and cannot be set.",

		// Labels
		[ScriptError.UnknownLabelJumpedTo] = "The specified label does not exist.",
		[ScriptError.LabelAlreadyDefined] = "A label with the same name was already defined on an earlier line.",

		// Strings
		[ScriptError.UnknownEscapeSequence] = "The specified escape sequence does not exist.",
		[ScriptError.InvalidStringInterpolation] = "The string interpolation expression is invalid: they can only contain valid identifiers and nested interpolation expressions. Other characters, including escape sequences, are not allowed.",
		[ScriptError.InterpolationNotClosed] = "The string interpolation (`\\(`) expression is not closed with a matching right parenthesis.",
	}.ToFrozenDictionary();

	// Invalid syntax
	public static InvalidScriptException InvalidSyntax(int line, string details) => new(line, ScriptError.InvalidSyntax, details);
	public static InvalidScriptException InvalidIdentifierName(int line, string identifier) => new(line, ScriptError.InvalidIdentifierName, $"`{identifier}` is not a valid identifier name.");
	public static InvalidScriptException LastArgumentNotClosed(int line) => new(line, ScriptError.LastArgumentNotClosed);

	// Invalid blocks
	public static InvalidScriptException NestedCommandDefined(int line, string outerCommandName, string nestedCommandName) => new(line, ScriptError.NestedCommandDefined, $"The command `{nestedCommandName}` is defined inside `{outerCommandName}`.");
	public static InvalidScriptException CommandFallenThrough(int line, string commandName, string? lastCalledCommand) => new(line, ScriptError.CommandFallenThrough, lastCalledCommand is null ? $"The body of the command `{commandName}` is empty." : $"The command `{commandName}` calls `{lastCalledCommand}` before ending.");
	public static InvalidScriptException BlockNotClosed(int line, string openingCommandName) => new(line, ScriptError.BlockNotClosed, $"The block `{openingCommandName}` is not closed.");

	// Language version declaration
	public static InvalidScriptException LanguageVersionNotSet() => new(-1, ScriptError.LanguageVersionNotSet);
	public static InvalidScriptException LanguageVersionSetTooLate(int line) => new(line, ScriptError.LanguageVersionSetTooLate);
	public static InvalidScriptException LanguageVersionAlreadySet(int line, int previousLine) => new(line, ScriptError.LanguageVersionAlreadySet, $"The language version was already set at line {previousLine}.");

	// Commands
	public static InvalidScriptException UnknownBuiltInCommandCalled(int line, string commandName) => new(line, ScriptError.UnknownBuiltInCommandCalled, $"No command named `{commandName}` exists.");
	public static InvalidScriptException UnknownUserDefinedCommandCalled(int line, string commandName) => new(line, ScriptError.UnknownUserDefinedCommandCalled, $"No command named `my {commandName}` exists. Did you forget to define this command with `command> {commandName};`?");
	public static InvalidScriptException UnknownConditionCalled(int line, string conditionName) => new(line, ScriptError.UnknownConditionCalled, $"No condition named `{conditionName}` exists.");
	public static InvalidScriptException UnknownSignatureCalled(int line, string commandName, int argumentCount) => new(line, ScriptError.UnknownSignatureCalled, $"The command `{commandName}` does not accept {argumentCount} arguments. See its documentation for more information.");
	public static InvalidScriptException InvalidArgumentsPassed(int line, string commandName, string details) => new(line, ScriptError.InvalidArgumentsPassed, $"The passed arguments are invalid for the command `{commandName}`: {details}");
	public static InvalidScriptException CommandAlreadyDefined(int line, string commandName, int previousLine) => new(line, ScriptError.CommandAlreadyDefined, $"`command> {commandName};` was already called on line {previousLine}.");

	// Variables
	public static InvalidScriptException UnknownVariableExpanded(int line, string variableName) => new(line, ScriptError.UnknownVariableExpanded, $"No variable named `{variableName}` exists.");
	public static InvalidScriptException ProtectedVariableSet(int line, string variableName) => new(line, ScriptError.ProtectedVariableSet, $"Cannot set the variable `{variableName}`: names starting with an underscore are reserved.");

	// Labels
	public static InvalidScriptException UnknownLabelJumpedTo(int line, string labelName) => new(line, ScriptError.UnknownLabelJumpedTo, $"No label named `{labelName}` exists.");
	public static InvalidScriptException LabelAlreadyDefined(int line, string labelName, int previousLine) => new(line, ScriptError.LabelAlreadyDefined, $"The label was already defined (`@{labelName}`) on line {previousLine}.");

	// Strings
	public static InvalidScriptException UnknownEscapeSequence(int line, string escapeSequence) => new(line, ScriptError.UnknownEscapeSequence, $"Escape sequence `\\{escapeSequence}` does not exist.");
	public static InvalidScriptException InvalidStringInterpolation(int line) => new(line, ScriptError.InvalidStringInterpolation);
	public static InvalidScriptException InterpolationNotClosed(int line) => new(line, ScriptError.InterpolationNotClosed);
}
