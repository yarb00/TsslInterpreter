// https://tssl.yarb00.dev

using System;

namespace TsslInterpreter;

internal sealed partial class ScriptEnvironment(string[] script)
{
	private interface IScriptExecutor
	{
		void ExecuteScript(ref int currentLine);
	}

	private readonly string[] script = script;

	private IScriptExecutor? executor;

	private int currentLine;

	public void ExecuteScript()
	{
		executor = null;
		currentLine = 0;

		while (currentLine < script.Length)
		{
			if (executor is not null)
			{
				try
				{
					executor.ExecuteScript(ref currentLine);
				}
				catch (InvalidCodeException e)
				{
					HandleInvalidCode(e);
				}
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
				catch (NotSupportedException)
				{
					Program.Panic(CriticalError.NotSupportedLanguageVersion);
				}
			}
		}
	}

	private void ExecuteInstruction(string instruction)
	{
		if (executor is not null) Program.Panic();

		instruction = instruction.TrimStart();

		if (instruction.IsEmpty || instruction.StartsWith('#')) return;

		if (!instruction.StartsWith("!TooSimpleScriptingLanguage", StringComparison.OrdinalIgnoreCase))
			throw new InvalidCodeException(currentLine, CodeError.LanguageVersionNotSet);

		string languageVersion = instruction[("!TooSimpleScriptingLanguage".Length + 1)..].Trim().ToUpperInvariant();

		executor = languageVersion switch
		{
			"0.5" => new ScriptEnvironmentV0_5(script),
			"0.6" => new ScriptEnvironmentV0_6(script),
			_ => throw new NotSupportedException()
		};
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
