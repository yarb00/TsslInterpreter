// https://yarb00.dev

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TsslInterpreter;

internal sealed partial class ScriptEnvironment
{
	private sealed class ScriptEnvironmentV0_4(int currentLine) : VersionedScriptEnvironment(currentLine)
	{
		protected override string LanguageVersion => "0.4";

		private readonly Dictionary<string, string> valueByName = [];
		private Dictionary<string, Action<string>> ActionByCommandName => new() // It's a field because property initializers don't like non-static references
		{
			["debug print values"] = DebugPrintValues,
			["debug print language version"] = DebugPrintLanguageVersion,
			["debug print interpreter version"] = DebugPrintInterpreterVersion,
			["debug print interpreter name"] = DebugPrintInterpreterName,
			["set value line"] = SetValueLine,
			["set value join"] = SetValueJoin,
			["print"] = Print,
			["print value"] = PrintValue,
			["print line"] = PrintLine,
			["print value line"] = PrintValueLine,
			["ask pause"] = AskPause,
			["ask value line"] = AskValueLine,
			["execute process"] = ExecuteProcess,
			["execute process value"] = ExecuteProcessValue,
			["execute process wait"] = ExecuteProcessWait,
			["execute process value wait"] = ExecuteProcessValueWait,
		};

		public override void RunInstruction(string instruction)
		{
			base.RunInstruction(instruction);

			if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#')) return;
			if (!instruction.Contains('>')) Error(CodeError.InvalidInstruction);

			if (instruction.Length > instruction.IndexOf('>') + 1 && instruction[instruction.IndexOf('>') + 1] != ' ') // If character after '>' is not space
				Error(CodeError.InvalidInstruction); // Require separating arguments with space

			string commandName = instruction[..instruction.IndexOf('>')].Trim().ToLowerInvariant(), commandArguments = string.Empty;

			if (!commandName.IsAlphaNumericWithSpaces()) Error(CodeError.InvalidCommandName);

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1) commandArguments = instruction[(instruction.IndexOf('>') + 1 + 1)..];

			if (ActionByCommandName.TryGetValue(commandName, out Action<string>? action)) action(commandArguments);
			else Error(CodeError.CommandNotFound);
		}

		#region Code handlers

		#region debug ...

		private void DebugPrintValues(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error(CodeError.NoArgumentsRequired);
			foreach ((string key, string value) in valueByName) PrintLine($"[\"{key}\"] = [\"{value}\"];");
		}
		private void DebugPrintLanguageVersion(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error(CodeError.NoArgumentsRequired);
			Print(LanguageVersion);
		}
		private void DebugPrintInterpreterVersion(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error(CodeError.NoArgumentsRequired);
			Print(Program.Version?.ToString(3) ?? "null");
		}
		private void DebugPrintInterpreterName(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error(CodeError.NoArgumentsRequired);
			Print(Program.Name);
		}

		#endregion

		#region set ...

		private void SetValueLine(string arguments)
		{
			if (!arguments.Contains(';') || !(arguments.Length > arguments.IndexOf(';') + 1)) Error(CodeError.InvalidArguments);

			string valueName = arguments[..arguments.IndexOf(';')], valueContent = arguments[(arguments.IndexOf(';') + 1)..];

			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.InvalidArguments);
			if (!valueName.IsAlphaNumericWithUnderscores()) Error(CodeError.InvalidValueName);

			valueByName[valueName] = valueContent;
		}

		private void SetValueJoin(string arguments)
		{
			if (!arguments.Contains(';') || !(arguments.Length > arguments.LastIndexOf(';') + 1)) Error(CodeError.InvalidArguments);

			string[] values = arguments.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (values.Length < 2) Error(CodeError.InvalidArguments);

			string receiverValue = values[0], resultString = string.Empty;
			if (!receiverValue.IsAlphaNumericWithUnderscores()) Error(CodeError.InvalidValueName);

			foreach (string senderValue in values[1..])
				if (!senderValue.IsAlphaNumericWithUnderscores()) Error(CodeError.InvalidValueName);
				else if (!valueByName.TryGetValue(senderValue, out string? senderValueContent)) Error(CodeError.ValueNotFound);
				else resultString += senderValueContent;

			SetValueLine($"{receiverValue};{resultString}");
		}

		#endregion

		#region print ...

		private void Print(string text)
		{
			if (text.IsNullOrEmpty()) Error(CodeError.ArgumentsRequired);
			else Console.Write(text);
		}

		private void PrintValue(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.InvalidValueName);
			else if (!valueByName.TryGetValue(valueName, out string? value)) Error(CodeError.ValueNotFound);
			else Print(value);
		}

		private static void PrintLine(string text)
		{
			if (text.IsNullOrEmpty()) Console.WriteLine();
			else Console.WriteLine(text);
		}

		private void PrintValueLine(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.InvalidValueName);
			else if (!valueByName.TryGetValue(valueName, out string? value)) Error(CodeError.ValueNotFound);
			else PrintLine(value);
		}

		#endregion

		#region ask ...

		private void AskPause(string _)
		{
			if (!_.IsNullOrWhiteSpace()) Error(CodeError.NoArgumentsRequired);
			Console.ReadKey(false);
		}

		private void AskValueLine(string valueName)
		{
			string? input = Console.ReadLine();
			if (input is null) Exit();
			else SetValueLine($"{valueName};{input}");
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
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.InvalidValueName);
			else if (!valueByName.TryGetValue(valueName, out string? value)) Error(CodeError.ValueNotFound);
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
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.InvalidValueName);
			else if (!valueByName.TryGetValue(valueName, out string? value)) Error(CodeError.ValueNotFound);
			else ExecuteProcessWait(value);
		}

		#endregion

		#endregion
	}
}
