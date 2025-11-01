// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;

namespace TsslInterpreter;

internal sealed partial class ScriptEnvironment
{
	private sealed class ScriptEnvironmentV0_5(string[] script) : IScriptExecutor
	{
		private const string languageVersion = "0.5";

		private readonly Dictionary<CodeError, string> messageByErrorType = new()
		{
			[CodeError.Unknown] = "Unknown error.",
			[CodeError.InvalidInstruction] = "Syntax error.",
			[CodeError.InvalidCommandName] = "Command name is not valid.",
			[CodeError.InvalidValueName] = "Value name is not valid.",
			[CodeError.InvalidLabelName] = "Label name is not valid.",
			[CodeError.CommandNotFound] = "Specified command is not found.",
			[CodeError.ValueNotFound] = "Specified value is not found.",
			[CodeError.LabelNotFound] = "Specified label is not found.",
			[CodeError.NoArgumentsRequired] = "Arguments were passed but command does not accept any.",
			[CodeError.ArgumentsRequired] = "No arguments were passed but command requires them.",
			[CodeError.InvalidArguments] = "Arguments are in invalid format or do not make sense."
		};

		private readonly Dictionary<string, int> lineByLabel = [];

		private readonly Dictionary<string, string> valueByName = new()
		{
			["_language_version"] = languageVersion,
			["_interpreter_version"] = Program.Version?.ToString(3) ?? "null",
			["_interpreter_name"] = Program.Name
		};

		private FrozenDictionary<string, Action<string>> ActionByCommandName => new Dictionary<string, Action<string>>() // It's a field because property initializers don't like non-static references
		{
			["jump"] = Jump,
			["jump if equals exact"] = JumpIfEqualsExact,
			["jump if equals expression"] = JumpIfEqualsExpression,

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
		}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

		private readonly string[] script = script;

		private int currentLine;

		private static void Exit() => Program.Exit();

		private static void Panic(CriticalError error, string? message = null) => Program.Panic(error, message);

		private void Error(CodeError error = CodeError.Unknown) => Panic(CriticalError.InvalidCode, $"Error on line {currentLine}: {messageByErrorType[error]}");

		public void ExecuteScript(ref int currentLine)
		{
			this.currentLine = currentLine;

			while (currentLine <= script.Length)
			{
				this.currentLine++;
				currentLine = this.currentLine;
				ExecuteInstruction(script[this.currentLine - 1]);
			}
		}

		private void ExecuteInstruction(string instruction)
		{
			instruction = instruction.TrimStart();

			if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#')) return;

			if (instruction.StartsWith("!TooSimpleScriptingLanguage", StringComparison.OrdinalIgnoreCase)) Error(CodeError.InvalidInstruction);

			if (instruction.StartsWith('@'))
			{
				string label = instruction[1..].Trim();

				if (!instruction.IsAlphaNumericWithSpaces()) Error(CodeError.InvalidLabelName);

				lineByLabel.Add(label, currentLine);

				return;
			}

			if (!instruction.Contains('>')) Error(CodeError.InvalidInstruction);

			if (instruction.Length > instruction.IndexOf('>') + 1 && instruction[instruction.IndexOf('>') + 1] != ' ') // If character after '>' is not space
				Error(CodeError.InvalidInstruction); // Require separating arguments with space

			string
				commandName = instruction[..instruction.IndexOf('>')].Trim().ToLowerInvariant(), // Before '>'
				commandArguments = string.Empty;

			if (!commandName.IsAlphaNumericWithSpaces()) Error(CodeError.InvalidCommandName);

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1) commandArguments = instruction[(instruction.IndexOf('>') + 1 + 1)..];

			if (ActionByCommandName.TryGetValue(commandName, out Action<string>? action)) action(commandArguments);
			else Error(CodeError.CommandNotFound);
		}

		#region Code handlers

		#region jump ...

		private void Jump(string arguments)
		{
			// ...
		}

		private void JumpIfEqualsExact(string arguments)
		{
			// ...
		}

		private void JumpIfEqualsExpression(string arguments)
		{
			// ...
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
