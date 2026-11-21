// https://tssl.yarb00.dev

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
	UnknownCommandCalled,
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
		[ScriptError.UnknownCommandCalled] = "The specified command does not exist.",
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
		[ScriptError.InterpolationNotClosed] = "The string interpolation (`\\(`) expression is not closed with a matching right parenthesis.",
	}.ToFrozenDictionary();
}
