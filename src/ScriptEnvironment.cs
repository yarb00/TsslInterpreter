// https://tssl.yarb00.dev

using System;

namespace TsslInterpreter;

internal enum CodeError
{
	Unknown,
	InvalidInstruction,
	InvalidCommandName, InvalidValueName, InvalidLabelName,
	CommandNotFound, ValueNotFound, LabelNotFound,
	NoArgumentsRequired, ArgumentsRequired, InvalidArguments
}

internal sealed partial class ScriptEnvironment(string[] script)
{
	private interface IScriptExecutor
	{
		void ExecuteScript(ref int currentLine);
	}

	private IScriptExecutor? executor = null;

	private readonly string[] script = script;

	private int currentLine = 0;

	public void ExecuteScript()
	{
		while (currentLine < script.Length)
		{
			if (executor is not null) executor.ExecuteScript(ref currentLine);
			else
			{
				currentLine++;
				ExecuteInstruction(script[currentLine - 1]);
			}
		}
	}

	private void ExecuteInstruction(string instruction)
	{
		if (executor is not null) Program.Panic();

		instruction = instruction.TrimStart();

		if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#')) return;

		if (instruction.StartsWith("!TooSimpleScriptingLanguage", StringComparison.OrdinalIgnoreCase))
		{
			switch (instruction[("!TooSimpleScriptingLanguage".Length + 1)..].Trim().ToLowerInvariant())
			{
				case "0.5": executor = new ScriptEnvironmentV0_5(script); break;

				default: Program.Panic(CriticalError.NotSupportedLanguageVersion); break;
			}
			return;
		}
		else Program.Panic(CriticalError.InvalidCode);
	}
}
