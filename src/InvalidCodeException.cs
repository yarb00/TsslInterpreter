// https://tssl.yarb00.dev

using System;
using System.Collections.Generic;

namespace TsslInterpreter;

internal enum CodeError
{
	Unknown,
	InvalidInstruction,
	InvalidCommandName, CommandNotFound, InvalidArguments, ArgumentsRequired, NoArgumentsRequired,
	InvalidValueName, ValueNotFound,
	InvalidLabelName, LabelNotFound, LabelAlreadyDefined
}

internal sealed class InvalidCodeException : Exception
{
	public int Line { get; private init; }

	public CodeError Reason { get; private init; }

	public string? Details { get; private init; }

	private static readonly Dictionary<CodeError, string> messageByCodeError = new()
	{
		[CodeError.Unknown] = "An error occurred",

		[CodeError.InvalidInstruction] = "Syntax is not valid",

		[CodeError.InvalidArguments] = "Passed arguments are in invalid format or do not make sense",
		[CodeError.ArgumentsRequired] = "No arguments were passed but command requires them",
		[CodeError.NoArgumentsRequired] = "Arguments were passed but command does not accept any",

		[CodeError.InvalidCommandName] = "Command name is not valid",
		[CodeError.InvalidValueName] = "Value name is not valid",
		[CodeError.InvalidLabelName] = "Label name is not valid",

		[CodeError.CommandNotFound] = "Specified command is not found",
		[CodeError.ValueNotFound] = "Specified value is not found",
		[CodeError.LabelNotFound] = "Specified label is not found",

		[CodeError.LabelAlreadyDefined] = "Label with this name is already defined"
	};

	private static readonly Dictionary<CodeError, string?> defaultDetailsByCodeError = new()
	{
		[CodeError.InvalidCommandName] = "Command name can only contain numbers, Latin letters and spaces",
		[CodeError.InvalidValueName] = "Value name can only contain numbers, Latin letters and underscores",
		[CodeError.InvalidLabelName] = "Label name can only contain numbers, Latin letters and underscores"
	};

	public InvalidCodeException(
		int line,
		CodeError reason = CodeError.Unknown,
		string? details = null
	) : this($"Error on line {line}: {messageByCodeError[reason]}.{GetDetails(reason, details)}") =>
		(Line, Reason, Details) = (line, reason, details);

	private InvalidCodeException() : base() { }

	private InvalidCodeException(string? message) : base(message) { }

	private static string GetDetails(CodeError reason, string? details)
	{
		if (details is null)
		{
			if (!defaultDetailsByCodeError.TryGetValue(reason, out string? value)) return string.Empty;

			details = value;
		}

		if (details is null) return string.Empty;

		return $" Details: \"{details}\"";
	}
}
