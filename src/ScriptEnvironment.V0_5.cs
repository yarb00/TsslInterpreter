// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

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
			["_interpreter_website"] = Program.Website,
			["_interpreter_name"] = Program.Name,
			["_interpreter_version"] = Program.Version?.ToString(3) ?? "null"
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
			ScanLabels();

			this.currentLine = currentLine;

			while (this.currentLine < script.Length)
			{
				this.currentLine++;
				currentLine = this.currentLine;
				ExecuteInstruction(script[this.currentLine - 1]);
			}
		}

		private void ScanLabels()
		{
			for (int i = 0; i < script.Length; i++)
			{
				string instruction = script[i].TrimStart();

				if (!instruction.StartsWith('@')) continue;

				string label = instruction[1..].Trim();

				if (!label.IsAlphaNumericWithUnderscores())
				{
					currentLine = i + 1; // Set current line to the line with label for proper error message in terminal
					Error(CodeError.InvalidLabelName);
				}

				lineByLabel.Add(label, i + 1);
			}
		}

		private void ExecuteInstruction(string instruction)
		{
			instruction = instruction.TrimStart();

			if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#') || instruction.StartsWith('@')) return;

			if (instruction.StartsWith("!TooSimpleScriptingLanguage", StringComparison.OrdinalIgnoreCase)) Error(CodeError.InvalidInstruction);

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

		private void Jump(string label)
		{
			if (label.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);
			else if (!lineByLabel.TryGetValue(label, out int line)) Error(CodeError.LabelNotFound);
			else currentLine = line;
		}

		private void JumpIfEqualsExact(string args)
		{
			if (args.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);

			string[] @params = args.Split(';');
			if (@params.Length != 3) Error(CodeError.InvalidArguments);
			(string label, string valueName, string compareValue) = (@params[0].Trim(), @params[1].Trim(), @params[2]);

			if (label.IsNullOrEmpty()) Error(CodeError.InvalidLabelName);
			if (valueName.IsNullOrEmpty()) Error(CodeError.InvalidValueName);

			if (!valueByName.TryGetValue(valueName, out string? valueContent)) Error(CodeError.ValueNotFound);

			if (valueContent == compareValue) Jump(label);
		}

		private void JumpIfEqualsExpression(string args)
		{
			if (args.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);

			string[] @params = args.Split(';');
			if (@params.Length != 3) Error(CodeError.InvalidArguments);
			(string label, string valueName, string expression) = (@params[0].Trim(), @params[1].Trim(), @params[2]);

			if (label.IsNullOrEmpty()) Error(CodeError.InvalidLabelName);
			if (valueName.IsNullOrEmpty()) Error(CodeError.InvalidValueName);

			if (!valueByName.TryGetValue(valueName, out string? valueContent)) Error(CodeError.ValueNotFound);
			if (valueContent is null)
			{
				Error();
				return;
			}

			if (Regex.IsMatch(valueContent, expression)) Jump(label);
		}

		#endregion

		#region set ...

		private void SetValueLine(string args)
		{
			if (args.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);

			string[] @params = args.Split(';');
			if (@params.Length != 2) Error(CodeError.InvalidArguments);
			(string valueName, string valueContent) = (@params[0].Trim(), @params[1]);

			if (valueName.IsNullOrEmpty() || !valueName.IsAlphaNumericWithUnderscores()) Error(CodeError.InvalidValueName);

			valueByName[valueName] = valueContent;
		}

		private void SetValueJoin(string args)
		{
			if (args.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);

			string[] @params = args.Split(';', StringSplitOptions.TrimEntries);
			if (@params.Contains(string.Empty) || @params.Length < 2) Error(CodeError.InvalidArguments);

			string receiverValue = @params[0], result = string.Empty;
			if (!receiverValue.IsAlphaNumericWithUnderscores()) Error(CodeError.InvalidValueName);

			foreach (string senderValue in @params[1..])
				if (!senderValue.IsAlphaNumericWithUnderscores()) Error(CodeError.InvalidValueName);
				else if (!valueByName.TryGetValue(senderValue, out string? senderValueContent)) Error(CodeError.ValueNotFound);
				else result += senderValueContent;

			SetValueLine($"{receiverValue};{result}");
		}

		#endregion

		#region print ...

		private void Print(string @string)
		{
			if (@string.IsNullOrEmpty()) Error(CodeError.ArgumentsRequired);
			else Console.Write(@string);
		}

		private void PrintValue(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) Error(CodeError.ValueNotFound);
			else Print(valueContent);
		}

		private static void PrintLine(string @string) => Console.WriteLine(@string);

		private void PrintValueLine(string valueName)
		{
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) Error(CodeError.ValueNotFound);
			else PrintLine(valueContent);
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
			if (!command.Trim().Contains(' ')) Process.Start(command);
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
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) Error(CodeError.ValueNotFound);
			else ExecuteProcess(valueContent);
		}

		private static void ExecuteProcessWait(string command)
		{
			string fileName = command;
			string? arguments = null;

			if (command.Trim().Contains(' '))
			{
				int argumentsStartIndex = command.Trim().IndexOf(' ') + 1; // Index of the next character after space
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
			if (valueName.IsNullOrWhiteSpace()) Error(CodeError.ArgumentsRequired);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) Error(CodeError.ValueNotFound);
			else ExecuteProcessWait(valueContent);
		}

		#endregion

		#endregion
	}
}
