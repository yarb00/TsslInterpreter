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

		SetValueCommand = "Usage: set value> [value name];[text]",

		PrintCommand = "Usage: print> [text]",
		PrintLineCommand = """
			Usage:
				print line>
				print line> [text]
			""",

		AskPauseCommand = "Usage: ask pause>",
		AskLineCommand = "Usage: ask line> [value name]",

		ExecuteCommand = """
			Usage:
				execute> [path to executable]
				execute> [path to executable] [arguments]
			""",
		ExecuteWaitCommand = """
			Usage:
				execute wait> [path to executable]
				execute wait> [path to executable] [arguments]
			""";

	public const string

		EqualsCondition = """
			Usage:
				?equals: [text 1];[text 2]
			""",
		MatchesCondition = """
			Usage:
				?matches: [text];[regex]
			""",

		NotEqualsCondition = """
			Usage:
				?not equals: [text 1];[text 2]
			""",
		NotMatchesCondition = """
			Usage:
				?not matches: [text];[regex]
			""";
}

internal sealed partial class ScriptEnvironment
{
	private sealed class ScriptEnvironmentV0_6(string[] script) : IScriptExecutor
	{
		private const string languageVersion = "0.6";

		private readonly Dictionary<string, int> lineByLabel = new(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, string> valueByName = new(StringComparer.OrdinalIgnoreCase)
		{
			["_language_version"] = languageVersion,

			["_interpreter_name"] = Program.FriendlyName,
			["_interpreter_website"] = Program.Website,
			["_interpreter_license"] = Program.License,
			["_interpreter_version"] = Program.FriendlyVersion,

			["_last_execute_status"] = string.Empty
		};

		private FrozenDictionary<string, Action<string[]>> CommandByName => new Dictionary<string, Action<string[]>>()
		{
			["set value"] = SetValue,

			["print"] = Print,
			["print line"] = PrintLine,

			["ask pause"] = AskPause,
			["ask line"] = AskLine,

			["execute"] = Execute,
			["execute wait"] = ExecuteWait
		}.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

		private FrozenDictionary<string, Func<string[], bool>> ConditionByName => new Dictionary<string, Func<string[], bool>>()
		{
			["equals"] = IsTrue_Equals,
			["matches"] = IsTrue_Matches,

			["not equals"] = IsTrue_NotEquals,
			["not matches"] = IsTrue_NotMatches
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

			if (instruction.StartsWith('<'))
				try
				{
					HandleJumpExpression(instruction);
				}
				catch
				{
					throw;
				}
			else
				try
				{
					HandleCommandExpression(instruction);
				}
				catch
				{
					throw;
				}
		}

		private void HandleCommandExpression(string instruction)
		{
			if (!instruction.Contains('>')) throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction);

			if (instruction.Length > instruction.IndexOf('>') + 1 /* Arguments are passed */ && instruction[instruction.IndexOf('>') + 1] != ' ' /* Arguments are not separated with a space character */)
				throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction, "Command's arguments must be separated with space.");

			string commandName = instruction[..instruction.IndexOf('>')].Trim(); // Before '>'
			string[] commandArguments = [];

			if (!commandName.IsAlphanumericWithSpaces)
				throw new InvalidCodeException(currentLine, CodeError.InvalidCommandName, $"Command \"{commandName}\" violates command naming rules: only numbers (0-9), Latin letters (A-Z) and spaces are permitted.");

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1)
				try
				{
					commandArguments = ParseArguments(instruction[(instruction.IndexOf('>') + 1 + 1)..] /* After '>' */);
				}
				catch
				{
					throw;
				}

			if (CommandByName.TryGetValue(commandName, out Action<string[]>? action))
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

		private void HandleJumpExpression(string instruction)
		{
			string label;
			string? condition;
			string[] arguments;

			try
			{
				(label, condition, arguments) = ParseJumpExpression(instruction);
			}
			catch
			{
				throw;
			}

			if (condition is null)
				try
				{
					Jump(label);
				}
				catch
				{
					throw;
				}
			else if (ConditionByName.TryGetValue(condition, out Func<string[], bool>? conditionAction))
				try
				{
					if (conditionAction(arguments)) Jump(label);
				}
				catch
				{
					throw;
				}
			else throw new InvalidCodeException(currentLine, CodeError.ConditionNotFound, $"There is no condition named \"{condition}\". Have you made a typo? Are you using the wrong language version (\"{languageVersion}\")?");
		}

		private void Jump(string label)
		{
			if (label.IsEmpty) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired);
			else if (!lineByLabel.TryGetValue(label, out int line)) throw new InvalidCodeException(currentLine, CodeError.LabelNotFound, $"There is no label named \"{label}\". Have you made a typo?");
			else currentLine = line;
		}

		private (string label, string? condition, string[] arguments) ParseJumpExpression(string instruction)
		{
			if (!instruction.StartsWith('<')) throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction);

			instruction = instruction[1..].TrimStart(); // Trim '<'

			if (!instruction.TrimEnd().Contains(' ')) // If jump instruction doesn't contain a condition
				return (instruction.TrimEnd(), null, []);

			string label = instruction[..instruction.IndexOf(' ')]; // Before ' '
			instruction = instruction[instruction.IndexOf(' ')..].TrimStart(); // Trim label

			if (!instruction.Contains('?') || !instruction.Contains(':')) throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction);

			string condition = instruction[(instruction.IndexOf('?') + 1)..instruction.IndexOf(':')].Trim();
			instruction = instruction[(instruction.IndexOf(':') + 1 + 1)..]; // Trim condition

			if (!condition.IsAlphanumericWithSpaces)
				throw new InvalidCodeException(currentLine, CodeError.InvalidConditionName, $"Condition \"{condition}\" violates condition naming rules: only numbers (0-9), Latin letters (A-Z) and spaces are permitted.");

			string[] arguments;

			try
			{
				arguments = ParseArguments(instruction);
			}
			catch
			{
				throw;
			}

			return (label, condition, arguments);
		}

		private string[] ParseArguments(string rawArguments) // "a;b1\;b2;\(var)\\c" where 'var' equals "test" => ["a", "b1;b2", @"test\c"]
		{
			string[] arguments = rawArguments.Split(';');
			List<string> temporary = [];
			for (int i = 0; i < arguments.Length; i++)
			{
				if (!arguments[i].EndsWith('\\'))
				{
					temporary.Add(arguments[i]);
					continue;
				}

				temporary.Add(arguments[i][..^1] + ';' + arguments[i + 1]);

				if (arguments.Length != i + 1)
					foreach (string @string in arguments[(i + 2)..])
						temporary.Add(@string);

				arguments = temporary.ToArray();
				temporary.Clear();

				i = -1;
			}

			for (int argumentIndex = 0; argumentIndex < arguments.Length; argumentIndex++)
			{
				string argument = arguments[argumentIndex], result = string.Empty;
				bool insideEscapeSequence = false;
				for (int i = 0; i < argument.Length; i++)
				{
					if (!insideEscapeSequence && argument[i] == '\\')
					{
						insideEscapeSequence = true;
						continue;
					}
					else if (insideEscapeSequence && argument[i] == '\\')
					{
						insideEscapeSequence = false;
						result += '\\';
					}
					else if (insideEscapeSequence && argument[i] == '(')
					{
						int endIndex = argument.IndexOf(')', i);
						string valueName = argument[(i + 1)..endIndex].Trim();

						if (!valueByName.TryGetValue(valueName, out string? valueContent))
							throw new InvalidCodeException(currentLine, CodeError.ValueNotFound, $"There is no value named \"{valueName}\". Have you made a typo?");

						result += valueContent;
						i = endIndex;
						insideEscapeSequence = false;

						continue;
					}
					else if (insideEscapeSequence) throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction, $"Escape sequence \"\\{argument[i]}\" does not exist. Have you made a typo?");
					else result += argument[i];
				}
				arguments[argumentIndex] = result;
			}

			return arguments;
		}

		#region Conditions

		private bool IsTrue_Equals(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.EqualsCondition);
			if (args.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.EqualsCondition);
			return args[0] == args[1];
		}

		private bool IsTrue_Matches(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.MatchesCondition);
			if (args.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.MatchesCondition);
			return Regex.IsMatch(args[0], args[1]);
		}

		private bool IsTrue_NotEquals(params string[] args)
		{
			try
			{
				return !IsTrue_Equals(args);
			}
			catch (InvalidCodeException e)
			{
				throw new InvalidCodeException(e.Line, e.Reason, Usage.NotEqualsCondition);
			}
		}

		private bool IsTrue_NotMatches(params string[] args)
		{
			try
			{
				return !IsTrue_Matches(args);
			}
			catch (InvalidCodeException e)
			{
				throw new InvalidCodeException(e.Line, e.Reason, Usage.NotMatchesCondition);
			}
		}

		#endregion

		#region Commands

		#region set ...

		private void SetValue(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.SetValueCommand);
			if (args.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.SetValueCommand);

			(string valueName, string valueContent) = (args[0].Trim(), args[1]);

			if (!valueName.IsAlphanumericWithUnderscores)
				throw new InvalidCodeException(currentLine, CodeError.InvalidValueName, $"Value \"{valueName}\" violates value naming rules: only numbers (0-9), Latin letters (A-Z) and underscores are permitted.");

			valueByName[valueName] = valueContent;
		}

		#endregion

		#region print ...

		private void Print(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.PrintCommand);
			else if (args.Length == 1) Console.Write(args[0]);
			else throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.PrintCommand);
		}

		private void PrintLine(params string[] args)
		{
			if (args.Length == 0) Console.WriteLine();
			else if (args.Length == 1) Console.WriteLine(args[0]);
			else throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.PrintLineCommand);
		}

		#endregion

		#region ask ...

		private void AskPause(params string[] args)
		{
			if (args.Length != 0) throw new InvalidCodeException(currentLine, CodeError.NoArgumentsRequired, Usage.AskPauseCommand);
			Console.ReadKey(false);
		}

		private void AskLine(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.AskLineCommand);
			if (args.Length != 1) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.AskLineCommand);

			string valueName = args[0];

			string? input = Console.ReadLine();
			if (input is null) Program.Exit();
			else
				try
				{
					SetValue(valueName, input);
				}
				catch
				{
					throw;
				}
		}

		#endregion

		#region execute ...

		private void Execute(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteCommand);
			if (args.Length != 1) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.ExecuteCommand);

			string command = args[0];

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

		private void ExecuteWait(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteWaitCommand);
			if (args.Length != 1) throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.ExecuteWaitCommand);

			string command = args[0];

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

		#endregion

		#endregion
	}
}
