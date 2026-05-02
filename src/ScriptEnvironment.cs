// https://tssl.yarb00.dev

using System;

namespace TsslInterpreter;

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
			if (executor is not null)
				try
				{
					executor.ExecuteScript(ref currentLine);
				}
				catch (InvalidCodeException e)
				{
					HandleInvalidCode(e);
				}
			else
			{
				currentLine++;
				try
				{
					ExecuteInstruction(script[currentLine - 1]);
				}
				catch (InvalidCodeException e)
				{
					HandleInvalidCode(e);
				}
			}
		}
	}

	private void ExecuteInstruction(string instruction)
	{
		if (executor is not null) Program.Panic();

		instruction = instruction.TrimStart();

		if (instruction.IsEmptyOrWhitespace || instruction.StartsWith('#')) return;

		if (instruction.StartsWith("!TooSimpleScriptingLanguage", StringComparison.OrdinalIgnoreCase))
		{
			switch (instruction[("!TooSimpleScriptingLanguage".Length + 1)..].Trim().ToLower())
			{
				case "0.5": executor = new ScriptEnvironmentV0_5(script); break;
				case "0.6": executor = new ScriptEnvironmentV0_6(script); break;

				default: Program.Panic(CriticalError.NotSupportedLanguageVersion); break;
			}
			return;
		}
		else throw new InvalidCodeException(currentLine, CodeError.LanguageVersionNotSet);
	}

	private static void HandleInvalidCode(InvalidCodeException e)
	{
		string message = $"""

			= Error on line {e.Line}: =
			{e.Message}
			""";

		if (e.Details is not null) message += $"""

			= Details: =
			{e.Details}
			""";

		Program.Panic(CriticalError.InvalidCode, message);
	}
}
