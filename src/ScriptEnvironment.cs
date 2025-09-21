// https://yarb00.dev

namespace TsslInterpreter;

internal abstract class VersionedScriptEnvironment
{
	protected abstract string LanguageVersion { get; }

	public abstract void RunInstruction(string instruction);

	protected static void Panic(CriticalError error) => Program.Panic(error);
	protected static void Error() => Panic(CriticalError.CodeError);
	protected static void Exit() => Program.Exit();
}

internal sealed partial class ScriptEnvironment
{
	private VersionedScriptEnvironment? scriptEnvironment = null;

	public void RunInstruction(string instruction)
	{
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
					case "0.2": scriptEnvironment = new ScriptEnvironmentV0_2(); break;
					case "0.3": scriptEnvironment = new ScriptEnvironmentV0_3(); break;
					default: Program.Panic(CriticalError.NotSupportedLanguageVersion); break;
				}
				return;
			}
		}
		else Program.Panic(CriticalError.CodeError);
	}
}
