// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace TsslInterpreter;

internal enum CodeError
{
	Unknown,
	InvalidInstruction,
	LanguageVersionNotSet, LanguageVersionAlreadySet,
	InvalidArguments, ArgumentsRequired, NoArgumentsRequired, InvalidArgumentCount,
	InvalidCommandName, InvalidValueName, InvalidLabelName, InvalidConditionName,
	CommandNotFound, ValueNotFound, LabelNotFound, ConditionNotFound, EscapeSequenceNotFound,
	LabelAlreadyDefined
}

internal sealed class InvalidCodeException : Exception
{
	public int Line { get; private init; }

	public CodeError Reason { get; private init; }

	public string? Details { get; private init; }

	private static readonly FrozenDictionary<CodeError, string> messageByCodeError = new Dictionary<CodeError, string>()
	{
		[CodeError.Unknown] = "An error occurred.",

		[CodeError.InvalidInstruction] = "Syntax is not valid.",

		[CodeError.LanguageVersionNotSet] = "Only comments (lines starting with \"#\") can be present before the language version declaration.",
		[CodeError.LanguageVersionAlreadySet] = "Language version is already set.",

		[CodeError.InvalidArguments] = "Arguments for the command/condition are in the invalid format or do not make sense.",
		[CodeError.ArgumentsRequired] = "No arguments were passed but the command/condition requires them.",
		[CodeError.NoArgumentsRequired] = "Arguments were passed but the command/condition does not accept any.",
		[CodeError.InvalidArgumentCount] = "Too many or too few arguments were passed to the command/condition.",

		[CodeError.InvalidCommandName] = "Specified command name is not valid.",
		[CodeError.InvalidValueName] = "Specified value name is not valid.",
		[CodeError.InvalidLabelName] = "Specified label name is not valid.",
		[CodeError.InvalidConditionName] = "Specified condition name is not valid.",

		[CodeError.CommandNotFound] = "Specified command is not found.",
		[CodeError.ValueNotFound] = "Specified value is not found.",
		[CodeError.LabelNotFound] = "Specified label is not found.",
		[CodeError.ConditionNotFound] = "Specified condition is not found.",
		[CodeError.EscapeSequenceNotFound] = "Specified escape sequence does not exist.",

		[CodeError.LabelAlreadyDefined] = "Label with the same name is already defined."
	}.ToFrozenDictionary();

	private InvalidCodeException() { }

	public InvalidCodeException(
		int line,
		CodeError reason = CodeError.Unknown,
		string? details = null
	) : base(messageByCodeError[reason]) => (Line, Reason, Details) = (line, reason, details);
}
