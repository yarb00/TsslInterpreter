// https://yarb00.dev

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TsslInterpreter;

internal sealed partial class ScriptEnvironment
{
	private sealed class ScriptEnvironmentV0_3 : VersionedScriptEnvironment
	{
		protected override string LanguageVersion => "0.3";

		private readonly Dictionary<string, string> valueByName = [];
		private Dictionary<string, Action<string>> ActionByCommandName => new() // It's a field because property initializers don't like non-static references
		{
			["debug print values"] = DebugPrintValues,
			["debug print language version"] = DebugPrintLanguageVersion,
			["debug print interpreter version"] = DebugPrintInterpreterVersion,
			["debug print interpreter name"] = DebugPrintInterpreterName,
			["set value"] = SetValue,
			["print"] = Print,
			["print value"] = PrintValue,
			["print line"] = PrintLine,
			["print value line"] = PrintValueLine,
			["ask value line"] = AskValueLine,
			["execute process"] = ExecuteProcess,
			["execute process value"] = ExecuteProcessValue,
			["execute process wait"] = ExecuteProcessWait,
			["execute process value wait"] = ExecuteProcessValueWait,
		};

		public override void RunInstruction(string instruction)
		{
			if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#')) return;
			if (!instruction.Contains('>')) Error();

			// Interpret command TRIMMING whitespace and IGNORING case
			string commandName = instruction[..instruction.IndexOf('>')].Trim().ToLowerInvariant(), commandArguments = string.Empty;

			if (!commandName.IsAlphaNumericWithSpaces()) Error();

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1) commandArguments = instruction[(instruction.IndexOf('>') + 1 + 1)..];
			else if (instruction.Length == instruction.IndexOf('>') + 1 + 1) Error();

			if (ActionByCommandName.TryGetValue(commandName, out Action<string>? action)) action(commandArguments);
			else Error();
		}

		#region Code handlers

		#region debug ...

		private void DebugPrintValues(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error();
			foreach ((string key, string value) in valueByName) PrintLine($"[\"{key}\"] = [\"{value}\"];");
		}
		private void DebugPrintLanguageVersion(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error();
			Print(LanguageVersion);
		}
		private static void DebugPrintInterpreterVersion(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error();
			Print(Program.Version?.ToString(3) ?? "null");
		}
		private static void DebugPrintInterpreterName(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error();
			Print(Program.Name);
		}

		#endregion

		#region set ...

		private void SetValue(string arguments)
		{
			if (!arguments.Contains(';') || !(arguments.Length > arguments.IndexOf(';') + 1)) Error();

			string valueName = arguments[..arguments.IndexOf(';')], valueContent = arguments[(arguments.IndexOf(';') + 1)..];

			if (valueName.IsNullOrWhiteSpace()) Error();
			if (!valueName.IsAlphaNumericWithUnderscores()) Error();

			valueByName[valueName] = valueContent;
		}

		#endregion

		#region print ...

		private static void Print(string text)
		{
			if (text.IsNullOrEmpty()) Error();
			else Console.Write(text);
		}

		private void PrintValue(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace() || !valueByName.TryGetValue(valueName, out string? value)) Error();
			else Print(value);
		}

		private static void PrintLine(string text)
		{
			if (text.IsNullOrEmpty()) Console.WriteLine();
			else Console.WriteLine(text);
		}

		private void PrintValueLine(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace() || !valueByName.TryGetValue(valueName, out string? value)) Error();
			else PrintLine(value);
		}

		#endregion

		#region ask ...

		private void AskValueLine(string valueName)
		{
			string? input = Console.ReadLine();
			if (input is null) Exit();
			else SetValue($"{valueName};{input}");
		}

		#endregion

		#region execute ...

		private static void ExecuteProcess(string command)
		{
			if (!command.Contains(' ')) Process.Start(command);
			else
			{
				int argumentsStartIndex = command.IndexOf(' ') + 1; // Index of the next character after space
				Process.Start(new ProcessStartInfo
				{
					UseShellExecute = true,
					FileName = command[..(argumentsStartIndex - 1)] /* Before space */,
					Arguments = command[argumentsStartIndex..] /* After space */
				});
			}
		}

		private void ExecuteProcessValue(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace() || !valueByName.TryGetValue(valueName, out string? value)) Error();
			else ExecuteProcess(value);
		}

		private static void ExecuteProcessWait(string command)
		{
			string fileName = command;
			string? arguments = null;

			if (command.Contains(' '))
			{
				int argumentsStartIndex = command.IndexOf(' ') + 1; // Index of the next character after space
				fileName = command[..(argumentsStartIndex - 1)]; // Before space
				arguments = command[argumentsStartIndex..]; // After space
			}

			ProcessStartInfo startInfo = new() { UseShellExecute = true, FileName = fileName };
			if (arguments is not null) startInfo.Arguments = arguments;

			Process process = new() { StartInfo = startInfo };
			process.Start();
			process.WaitForExit();
		}

		private void ExecuteProcessValueWait(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace() || !valueByName.TryGetValue(valueName, out string? value)) Error();
			else ExecuteProcessWait(value);
		}

		#endregion

		#endregion
	}
}
