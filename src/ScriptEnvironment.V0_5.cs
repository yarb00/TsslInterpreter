// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TsslInterpreter;

file static class Usage
{
	public const string

		JumpCommand = "Usage: jump> [label]",
		JumpIfEqualsExactCommand = "Usage: jump if equals exact> [label];[value name];[text]",
		JumpIfEqualsExpressionCommand = "Usage: jump if equals expression> [label];[value name];[regex]",

		SetValueLineCommand = "Usage: set value line> [value name];[text]",
		SetValueJoinCommand = "Usage: set value join> [result value name];[value 1];[value 2];[value X];...",

		PrintCommand = """
			Usage:
				print>
				print> [text]
			""",
		PrintValueCommand = "Usage: print value> [value name]",
		PrintLineCommand = """
			Usage:
				print line>
				print line> [text]
			""",
		PrintValueLineCommand = "Usage: print value line> [value name]",

		AskPauseCommand = "Usage: ask pause>",
		AskValueLineCommand = "Usage: ask value line> [value name]",

		ExecuteProcessCommand = """
			Usage:
				execute process> [path to executable]
				execute process> [path to executable] [arguments]
			""",
		ExecuteProcessValueCommand = "Usage: execute process value> [value name]",
		ExecuteProcessWaitCommand = """
			Usage:
				execute process wait> [path to executable]
				execute process wait> [path to executable] [arguments]
			""",
		ExecuteProcessValueWaitCommand = "Usage: execute process value wait> [value name]";
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

			["_interpreter_name"] = Program.FriendlyName,
			["_interpreter_website"] = Program.Website,
			["_interpreter_version"] = Program.FriendlyVersion
		};

		private FrozenDictionary<string, Action<string>> CommandByName => new Dictionary<string, Action<string>>()
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
					throw new InvalidCodeException(i + 1, CodeError.InvalidLabelName, $"Label \"{label}\" violates label naming rules: only numbers (0-9), Latin letters (A-Z) and underscores are permitted.");

				if (lineByLabel.TryGetValue(label, out int line))
					throw new InvalidCodeException(i + 1, CodeError.LabelAlreadyDefined, $"Label \"{label}\" was already defined at the line {line}.");

				lineByLabel.Add(label, i + 1);
			}
		}

		private void ExecuteInstruction(string instruction)
		{
			instruction = instruction.TrimStart();

			if (instruction.IsEmpty || instruction.StartsWith('#') || instruction.StartsWith('@')) return;

			if (instruction.StartsWith("!TooSimpleScriptingLanguage", StringComparison.OrdinalIgnoreCase))
				throw new InvalidCodeException(currentLine, CodeError.LanguageVersionAlreadySet, $"Encountered \"{instruction}\", but language version is already set to \"{languageVersion}\".");

			if (!instruction.Contains('>')) throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction);

			if (instruction.Length > instruction.IndexOf('>') + 1 /* Arguments are passed */ && instruction[instruction.IndexOf('>') + 1] != ' ' /* Arguments are not separated with a space character */)
				throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction, "Command's arguments must be separated with space.");

			string
				commandName = instruction[..instruction.IndexOf('>')].Trim(), // Before '>'
				commandArguments = string.Empty;

			if (!commandName.IsAlphanumericWithSpaces)
				throw new InvalidCodeException(currentLine, CodeError.InvalidCommandName, $"Command \"{commandName}\" violates command naming rules: only numbers (0-9), Latin letters (A-Z) and spaces are permitted.");

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1) commandArguments = instruction[(instruction.IndexOf('>') + 1 + 1)..]; // After '>'

			if (CommandByName.TryGetValue(commandName, out Action<string>? action))
				try
				{
					action(commandArguments);
				}
				catch
				{
					throw;
				}
			else throw new InvalidCodeException(currentLine, CodeError.CommandNotFound, $"There is no command named \"{commandName}\". Have you made a typo? Are you using the wrong language version (\"{languageVersion}\")?");
		}

		#region Commands

		#region jump ...

		private void Jump(string label)
		{
			if (label.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.JumpCommand);
			else if (!lineByLabel.TryGetValue(label, out int line)) throw new InvalidCodeException(currentLine, CodeError.LabelNotFound, $"There is no label named \"{label}\". Have you made a typo?");
			else currentLine = line;
		}

		private void JumpIfEqualsExact(string args)
		{
			if (args.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.JumpIfEqualsExactCommand);

			string[] @params = args.Split(';');
			if (@params.Length != 3) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.JumpIfEqualsExactCommand);
			(string label, string valueName, string compareString) = (@params[0].Trim(), @params[1].Trim(), @params[2]);

			if (label.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.JumpIfEqualsExactCommand);
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.JumpIfEqualsExactCommand);

			if (!valueByName.TryGetValue(valueName, out string? valueContent))
				throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{valueName}\". Have you made a typo?");

			if (valueContent == compareString)
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
			if (args.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.JumpIfEqualsExpressionCommand);

			string[] @params = args.Split(';');
			if (@params.Length != 3) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.JumpIfEqualsExpressionCommand);
			(string label, string valueName, string expression) = (@params[0].Trim(), @params[1].Trim(), @params[2]);

			if (label.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.JumpIfEqualsExpressionCommand);
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.JumpIfEqualsExpressionCommand);

			if (!valueByName.TryGetValue(valueName, out string? valueContent))
				throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{valueName}\". Have you made a typo?");

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
			if (args.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.SetValueLineCommand);

			string[] @params = args.Split(';');
			if (@params.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.SetValueLineCommand);
			(string valueName, string valueContent) = (@params[0].Trim(), @params[1]);

			if (!valueName.IsAlphanumericWithUnderscores)
				throw new InvalidCodeException(currentLine, CodeError.InvalidValueName, $"Value \"{valueName}\" violates value naming rules: only numbers (0-9), Latin letters (A-Z) and underscores are permitted.");

			valueByName[valueName] = valueContent;
		}

		private void SetValueJoin(string args)
		{
			if (args.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.SetValueJoinCommand);

			string[] @params = args.Split(';', StringSplitOptions.TrimEntries);
			if (@params.Length < 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.SetValueJoinCommand);

			string receiverValue = @params[0], result = string.Empty;

			foreach (string senderValue in @params[1..])
				if (!valueByName.TryGetValue(senderValue, out string? senderValueContent))
					throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{senderValue}\". Have you made a typo?");
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
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.PrintValueCommand);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent))
				throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{valueName}\". Have you made a typo?");
			else Print(valueContent);
		}

		private static void PrintLine(string @string) => Console.WriteLine(@string);

		private void PrintValueLine(string valueName)
		{
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.PrintValueLineCommand);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent))
				throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{valueName}\". Have you made a typo?");
			else PrintLine(valueContent);
		}

		#endregion

		#region ask ...

		private void AskPause(string _)
		{
			if (!_.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.NoArgumentsRequired, Usage.AskPauseCommand);
			Console.ReadKey(false);
		}

		private void AskValueLine(string valueName)
		{
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.AskValueLineCommand);

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
			if (command.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteProcessCommand);

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
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteProcessValueCommand);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent))
				throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{valueName}\". Have you made a typo?");
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
			if (command.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteProcessWaitCommand);

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
			if (valueName.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteProcessValueWaitCommand);
			else if (!valueByName.TryGetValue(valueName, out string? valueContent))
				throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{valueName}\". Have you made a typo?");
			else
				try
				{
					ExecuteProcessWait(valueContent);
				}
				catch
				{
					throw;
				}
		}

		#endregion

		#endregion
	}
}
