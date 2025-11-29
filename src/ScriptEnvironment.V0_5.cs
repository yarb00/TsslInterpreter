// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace TsslInterpreter;

file static class CommandUsage
{
	public const string
		Jump = "Usage: jump> [label]",
		JumpIfEqualsExact = "Usage: jump if equals exact> [label];[value name];[text]",
		JumpIfEqualsExpression = "Usage: jump if equals expression> [label];[value name];[regex]",

		SetValueLine = "Usage: set value line> [value name];[text]",
		SetValueJoin = "Usage: set value join> [result value name];[value1];[value2];[valueX];...",

		Print = "Usage: print> [text]",
		PrintValue = "Usage: print value> [value name]",
		PrintLine = "Usage: 'print line>', 'print line> [text]'",
		PrintValueLine = "Usage: print value line> [value name]",

		AskPause = "Usage: ask pause>",
		AskValueLine = "Usage: ask value line> [value name]",

		ExecuteProcess = "Usage: execute process> [text]",
		ExecuteProcessValue = "Usage: execute process value> [value name]",
		ExecuteProcessWait = "Usage: execute process wait> [text]",
		ExecuteProcessValueWait = "Usage: execute process value wait> [value name]";
}

internal sealed partial class ScriptEnvironment
{
	private sealed class ScriptEnvironmentV0_5(string[] script) : IScriptExecutor
	{
		private const string languageVersion = "0.5";

		private readonly Dictionary<string, int> lineByLabel = new(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, string> valueByName = new(StringComparer.OrdinalIgnoreCase)
		{
			["_language_version"] = languageVersion,
			["_interpreter_name"] = Program.Name,
			["_interpreter_website"] = Program.Website,
			["_interpreter_version"] = Program.FriendlyVersion
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

		public void ExecuteScript(ref int currentLine)
		{
			if (lineByLabel.Count == 0)
				try
				{
					ScanLabels();
				}
				catch
				{
					throw;
				}

			this.currentLine = currentLine;

			while (this.currentLine < script.Length)
			{
				this.currentLine++;

				try
				{
					ExecuteInstruction(script[this.currentLine - 1]);
				}
				catch
				{
					throw;
				}
			}

			currentLine = this.currentLine;
		}

		private void ScanLabels()
		{
			for (int i = 0; i < script.Length; i++)
			{
				string instruction = script[i].TrimStart();

				if (!instruction.StartsWith('@')) continue;

				string label = instruction[1..].Trim();

				if (!label.IsAlphanumericWithUnderscores)
				{
					currentLine = i + 1;
					throw new InvalidCodeException(currentLine, CodeError.InvalidLabelName);
				}

				if (lineByLabel.ContainsKey(label))
				{
					currentLine = i + 1;
					throw new InvalidCodeException(currentLine, CodeError.LabelAlreadyDefined);
				}

				lineByLabel.Add(label, i + 1);
			}
		}

		private void ExecuteInstruction(string instruction)
		{
			instruction = instruction.TrimStart();

			if (instruction.IsEmptyOrWhitespace || instruction.StartsWith('#') || instruction.StartsWith('@')) return;

			if (instruction.StartsWith("!TooSimpleScriptingLanguage", StringComparison.OrdinalIgnoreCase)) throw new InvalidCodeException(currentLine, details: "Language version is already set");

			if (!instruction.Contains('>')) throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction);

			if (instruction.Length > instruction.IndexOf('>') + 1 && instruction[instruction.IndexOf('>') + 1] != ' ') // If character after '>' is not space
				throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction, "Command's arguments must be separated with space");

			string
				commandName = instruction[..instruction.IndexOf('>')].Trim(), // Before '>'
				commandArguments = string.Empty;

			if (!commandName.IsAlphanumericWithSpaces) throw new InvalidCodeException(currentLine, CodeError.InvalidCommandName);

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1) commandArguments = instruction[(instruction.IndexOf('>') + 1 + 1)..]; // After '>'

			if (ActionByCommandName.TryGetValue(commandName, out Action<string>? action))
				try
				{
					action(commandArguments);
				}
				catch
				{
					throw;
				}
			else throw new InvalidCodeException(currentLine, CodeError.CommandNotFound);
		}

		#region Code handlers

		#region jump ...

		private void Jump(string label)
		{
			if (label.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.Jump);
			else if (!lineByLabel.TryGetValue(label, out int line)) throw new InvalidCodeException(currentLine, CodeError.LabelNotFound);
			else currentLine = line;
		}

		private void JumpIfEqualsExact(string args)
		{
			if (args.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.JumpIfEqualsExact);

			string[] @params = args.Split(';');
			if (@params.Length != 3) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, CommandUsage.JumpIfEqualsExact);
			(string label, string valueName, string compareValue) = (@params[0].Trim(), @params[1].Trim(), @params[2]);

			if (label.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidLabelName);
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidValueName);

			if (!valueByName.TryGetValue(valueName, out string? valueContent)) throw new InvalidCodeException(currentLine, CodeError.ValueNotFound);

			if (valueContent == compareValue)
				try
				{
					Jump(label);
				}
				catch
				{
					throw;
				}
		}

		private void JumpIfEqualsExpression(string args)
		{
			if (args.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.JumpIfEqualsExpression);

			string[] @params = args.Split(';');
			if (@params.Length != 3) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, CommandUsage.JumpIfEqualsExpression);
			(string label, string valueName, string expression) = (@params[0].Trim(), @params[1].Trim(), @params[2]);

			if (label.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidLabelName);
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidValueName);

			if (!valueByName.TryGetValue(valueName, out string? valueContent)) throw new InvalidCodeException(currentLine, CodeError.ValueNotFound);
			if (valueContent is null) throw new InvalidCodeException(currentLine);

			if (Regex.IsMatch(valueContent, expression))
				try
				{
					Jump(label);
				}
				catch
				{
					throw;
				}
		}

		#endregion

		#region set ...

		private void SetValueLine(string args)
		{
			if (args.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.SetValueLine);

			string[] @params = args.Split(';');
			if (@params.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, CommandUsage.SetValueLine);
			(string valueName, string valueContent) = (@params[0].Trim(), @params[1]);

			if (valueName.IsEmpty || !valueName.IsAlphanumericWithUnderscores) throw new InvalidCodeException(currentLine, CodeError.InvalidValueName);

			valueByName[valueName] = valueContent;
		}

		private void SetValueJoin(string args)
		{
			if (args.IsEmptyOrWhitespace) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.SetValueJoin);

			string[] @params = args.Split(';', StringSplitOptions.TrimEntries);
			if (@params.Contains(string.Empty) || @params.Length < 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, CommandUsage.SetValueJoin);

			string receiverValue = @params[0], result = string.Empty;
			if (!receiverValue.IsAlphanumericWithUnderscores) throw new InvalidCodeException(currentLine, CodeError.InvalidValueName);

			foreach (string senderValue in @params[1..])
				if (!senderValue.IsAlphanumericWithUnderscores) throw new InvalidCodeException(currentLine, CodeError.InvalidValueName);
				else if (!valueByName.TryGetValue(senderValue, out string? senderValueContent)) throw new InvalidCodeException(currentLine, CodeError.ValueNotFound);
				else result += senderValueContent;

			try
			{
				SetValueLine($"{receiverValue};{result}");
			}
			catch
			{
				throw;
			}
		}

		#endregion

		#region print ...

		private void Print(string @string) => Console.Write(@string);

		private void PrintValue(string valueName)
		{
			if (valueName.IsEmptyOrWhitespace) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.PrintValue);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) throw new InvalidCodeException(currentLine, CodeError.ValueNotFound);
			else Print(valueContent);
		}

		private static void PrintLine(string @string) => Console.WriteLine(@string);

		private void PrintValueLine(string valueName)
		{
			if (valueName.IsEmptyOrWhitespace) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.PrintValueLine);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) throw new InvalidCodeException(currentLine, CodeError.ValueNotFound);
			else PrintLine(valueContent);
		}

		#endregion

		#region ask ...

		private void AskPause(string _)
		{
			if (!_.IsEmptyOrWhitespace) throw new InvalidCodeException(currentLine, CodeError.NoArgumentsRequired, CommandUsage.AskPause);
			Console.ReadKey(false);
		}

		private void AskValueLine(string valueName)
		{
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.AskValueLine);

			string? input = Console.ReadLine();
			if (input is null) Program.Exit();
			else
				try
				{
					SetValueLine($"{valueName};{input}");
				}
				catch
				{
					throw;
				}
		}

		#endregion

		#region execute ...

		private void ExecuteProcess(string command)
		{
			if (command.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.ExecuteProcess);

			if (!command.Trim().Contains(' ')) Process.Start(command);
			else
			{
				int argumentsStartIndex = command.IndexOf(' ') + 1; // Index of the next character after space
				Process.Start(new ProcessStartInfo
				{
					UseShellExecute = true,
					FileName = command[..(argumentsStartIndex - 1)], // Before space
					Arguments = command[argumentsStartIndex..] // After space
				});
			}
		}

		private void ExecuteProcessValue(string valueName)
		{
			if (valueName.IsEmptyOrWhitespace) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.ExecuteProcessValue);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) throw new InvalidCodeException(currentLine, CodeError.ValueNotFound);
			else
				try
				{
					ExecuteProcess(valueContent);
				}
				catch
				{
					throw;
				}
		}

		private void ExecuteProcessWait(string command)
		{
			if (command.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.ExecuteProcessWait);

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
			if (valueName.IsEmptyOrWhitespace) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, CommandUsage.ExecuteProcessValueWait);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent)) throw new InvalidCodeException(currentLine, CodeError.ValueNotFound);
			else ExecuteProcessWait(valueContent);
		}

		#endregion

		#endregion
	}
}
