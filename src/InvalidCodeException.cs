// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace TsslInterpreter;

internal enum CodeError
{
	Unknown,
	InvalidInstruction,
	LanguageVersionNotSet,
	LanguageVersionAlreadySet,
	InvalidArguments, ArgumentsRequired, NoArgumentsRequired,
	InvalidCommandName, InvalidValueName, InvalidLabelName,
	CommandNotFound, ValueNotFound, LabelNotFound,
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

		[CodeError.LanguageVersionNotSet] = "Only comments can be present before the language version declaration.",
		[CodeError.LanguageVersionAlreadySet] = "Language version is already set.",

		[CodeError.InvalidArguments] = "Arguments are in the invalid format or do not make sense.",
		[CodeError.ArgumentsRequired] = "No arguments were passed but command requires them.",
		[CodeError.NoArgumentsRequired] = "Arguments were passed but command does not accept any.",

		[CodeError.InvalidCommandName] = "Command name is not valid.",
		[CodeError.InvalidValueName] = "Value name is not valid.",
		[CodeError.InvalidLabelName] = "Label name is not valid.",

		[CodeError.CommandNotFound] = "Specified command is not found.",
		[CodeError.ValueNotFound] = "Specified value is not found.",
		[CodeError.LabelNotFound] = "Specified label is not found.",

		[CodeError.LabelAlreadyDefined] = "Label with the same name is already defined."
	}.ToFrozenDictionary();

	private InvalidCodeException() : base() { }

	public InvalidCodeException(
		int line,
		CodeError reason = CodeError.Unknown,
		string? details = null
	) : base(messageByCodeError[reason]) => (Line, Reason, Details) = (line, reason, details);
}
