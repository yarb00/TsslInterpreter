// https://tssl.yarb00.dev

using System.Collections.Generic;

namespace TsslInterpreter;

internal enum CodeError
{
	Other,
	InvalidInstruction,
	InvalidCommandName, InvalidValueName,
	CommandNotFound, ValueNotFound,
	NoArgumentsRequired, ArgumentsRequired, InvalidArguments
}

internal abstract class VersionedScriptEnvironment(int currentLine)
{
	protected abstract string LanguageVersion { get; }
	private int CurrentLine { get; set; } = currentLine;

	private readonly Dictionary<CodeError, string> errorMessageByErrorType = new()
	{
		[CodeError.Other] = "[Not present.]",
		[CodeError.InvalidInstruction] = "Syntax error.",
		[CodeError.InvalidCommandName] = "Command name is not valid.",
		[CodeError.InvalidValueName] = "Value name is not valid.",
		[CodeError.CommandNotFound] = "Specified command is not found.",
		[CodeError.ValueNotFound] = "Specified value is not found.",
		[CodeError.NoArgumentsRequired] = "Arguments were passed but command does not accept any.",
		[CodeError.ArgumentsRequired] = "No arguments were passed but command requires them.",
		[CodeError.InvalidArguments] = "Arguments are in invalid format or do not make sense."
	};

	public virtual void RunInstruction(string instruction) => CurrentLine++;

	protected static void Panic(CriticalError error, string? message = null) => Program.Panic(error, message);
	protected void Error(CodeError error) => Panic(CriticalError.CodeError, $"Error on line {CurrentLine}, details: {errorMessageByErrorType[error]}");
	protected static void Exit() => Program.Exit();
}

internal sealed partial class ScriptEnvironment
{
	private VersionedScriptEnvironment? scriptEnvironment = null;
	private int CurrentLine = 0;

	public void RunInstruction(string instruction)
	{
		CurrentLine++;

		if (scriptEnvironment is not null)
		{
			scriptEnvironment.RunInstruction(instruction);
			return;
		}

		if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#')) return;

		if (instruction.StartsWith("!TooSimpleScriptingLanguage"))
		{
			if (scriptEnvironment is not null) Program.Panic(CriticalError.CodeError);
			else
			{
				switch (instruction[("!TooSimpleScriptingLanguage".Length + 1)..].Trim().ToLowerInvariant())
				{
					case "0.3": scriptEnvironment = new ScriptEnvironmentV0_3(CurrentLine); break;
					case "0.4": scriptEnvironment = new ScriptEnvironmentV0_4(CurrentLine); break;
					default: Program.Panic(CriticalError.NotSupportedLanguageVersion); break;
				}
				return;
			}
		}
		else Program.Panic(CriticalError.CodeError);
	}
}
