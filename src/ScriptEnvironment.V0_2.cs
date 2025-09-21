// https://yarb00.dev

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TsslInterpreter;

internal sealed partial class ScriptEnvironment
{
	private sealed class ScriptEnvironmentV0_2 : VersionedScriptEnvironment
	{
		protected override string LanguageVersion => "0.2";

		private readonly Dictionary<string, string> valueByName = [];
		private Dictionary<string, Action<string>> ActionByCommandName => new() // It's a field because property initializers don't like non-static references
		{
			["print"] = Print,
			["print value"] = PrintValue,
			["print line"] = PrintLine,
			["print line value"] = PrintLineValue,
			["ask line value"] = AskLineValue,
			["execute process"] = ExecuteProcess,
			["execute process wait"] = ExecuteProcessWait,
		};

		public override void RunInstruction(string instruction)
		{
			if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#')) return;
			if (instruction.StartsWith('!')) Error();
			if (!instruction.Contains('>')) Error();

			string commandName = instruction[..instruction.IndexOf('>')], commandArguments = string.Empty;

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1) commandArguments = instruction[(instruction.IndexOf('>') + 1 + 1)..];
			else if (instruction.Length == instruction.IndexOf('>') + 1 + 1) Error();

			// Search for command PRESERVING whitespace and case
			if (ActionByCommandName.TryGetValue(commandName, out Action<string>? action)) action(commandArguments);
			else Error();
		}

		#region Code handlers

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

		private void PrintLineValue(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace() || !valueByName.TryGetValue(valueName, out string? value)) Error();
			else PrintLine(value);
		}

		#endregion

		#region ask ...

		private void AskLineValue(string valueName)
		{
			string? input = Console.ReadLine();
			if (input is null) Exit();
			else valueByName[valueName] = input;
		}

		#endregion

		#region execute ...

		private static void ExecuteProcess(string command)
		{
			if (!command.Contains(' ')) Process.Start(command);
			else
			{
				int argumentsStartIndex = command.IndexOf(' ') + 1; // Index of the next character after space
				Process.Start(command[..(argumentsStartIndex - 1)] /* Before space */, command[argumentsStartIndex..] /* After space */);
			}
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

			ProcessStartInfo startInfo = new() { FileName = fileName };
			if (arguments is not null) startInfo.Arguments = arguments;

			Process process = new() { StartInfo = startInfo };
			process.Start();
			process.WaitForExit();
		}

		#endregion

		#endregion
	}
}
