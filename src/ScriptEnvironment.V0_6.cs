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

		SetValueCommand = """
			The "set value" command. Accepts 2 arguments. Usage:
				set value> [value name];[text]
			""",

		PrintCommand = """
			The "print" command. Accepts 1 argument. Usage:
				print> [text]
			""",
		PrintLineCommand = """
			The "print line" command. Accepts 1 argument or no arguments. Usage:
				print line>
				print line> [text]
			""",

		AskPauseCommand = """
			The "ask pause" command. Accepts no arguments. Usage:
				ask pause>
			""",
		AskLineCommand = """
			The "ask line" command. Accepts 1 argument. Usage:
				ask line> [value name]
			""",

		ExecuteCommand = """
			The "execute" command. Accepts 1 argument. Usage:
				execute> [path to executable]
				execute> [path to executable] [arguments]
			""",
		ExecuteWaitCommand = """
			The "execute wait" command. Accepts 1 argument. Usage:
				execute wait> [path to executable]
				execute wait> [path to executable] [arguments]
			""";

	public const string

		EqualsCondition = """
			The "equals" condition. Accepts 2 arguments. Usage:
				?equals: [text 1];[text 2]
			""",
		MatchesCondition = """
			The "matches" condition. Accepts 2 arguments. Usage:
				?matches: [text];[regex]
			""",

		NotEqualsCondition = """
			The "not equals" condition. Accepts 2 arguments. Usage:
				?not equals: [text 1];[text 2]
			""",
		NotMatchesCondition = """
			The "not matches" condition. Accepts 2 arguments. Usage:
				?not matches: [text];[regex]
			""";

	public static string WithMessage(string usage, string message) => $"""
		{message}

		More info about:
		{usage}
		""";
}

partial class ScriptEnvironment
{
	private sealed class ScriptEnvironmentV0_6(string[] script) : IScriptExecutor
	{
		private const string languageVersion = "0.6";

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

		private readonly string[] script = script;

		private int currentLine;

		public void ExecuteScript(ref int currentLine)
		{
			if (lineByLabel.Count == 0) ScanLabels();

			this.currentLine = currentLine;

			while (this.currentLine < script.Length)
			{
				this.currentLine++;

				ExecuteInstruction(script[this.currentLine - 1]);
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

			if (instruction.StartsWith('<')) HandleJumpExpression(instruction);
			else if (instruction.Contains('>')) HandleCommandExpression(instruction);
			else throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction);
		}

		private void HandleCommandExpression(string instruction)
		{
			if (!instruction.Contains('>')) Program.Panic();

			if (instruction.Length > instruction.IndexOf('>') + 1 /* Arguments are passed */ && instruction[instruction.IndexOf('>') + 1] != ' ' /* Arguments are not separated with a space character */)
				throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction, "Command's arguments must be separated with a space. Insert a space character after '>'.");

			string commandName = instruction[..instruction.IndexOf('>')].Trim(); // Before '>'
			string[] commandArguments = [];

			if (!commandName.IsAlphanumericWithSpaces)
				throw new InvalidCodeException(currentLine, CodeError.InvalidCommandName, $"Command \"{commandName}\" violates command naming rules: only numbers (0-9), Latin letters (A-Z) and spaces are permitted.");

			if (instruction.Length > instruction.IndexOf('>') + 1 + 1 /* Arguments are passed */) commandArguments = ParseArguments(instruction[(instruction.IndexOf('>') + 1 + 1)..] /* After '>' */);

			if (CommandByName.TryGetValue(commandName, out Action<string[]>? action)) action(commandArguments);
			else throw new InvalidCodeException(currentLine, CodeError.CommandNotFound, $"There is no command named \"{commandName}\". Have you made a typo? Are you using the wrong language version (\"{languageVersion}\")?");
		}

		private void HandleJumpExpression(string instruction)
		{
			string label;
			string? condition;
			string[] arguments;

			(label, condition, arguments) = ParseJumpExpression(instruction);

			if (condition is null) Jump(label);
			else if (!ConditionByName.TryGetValue(condition, out Func<string[], bool>? conditionAction))
				throw new InvalidCodeException(currentLine, CodeError.ConditionNotFound, $"There is no condition named \"{condition}\". Have you made a typo? Are you using the wrong language version (\"{languageVersion}\")?");
			else if (conditionAction(arguments)) Jump(label);
		}

		private void Jump(string label)
		{
			if (!lineByLabel.TryGetValue(label, out int line)) throw new InvalidCodeException(currentLine, CodeError.LabelNotFound, $"There is no label named \"{label}\". Have you made a typo?");
			else currentLine = line;
		}

		private (string label, string? condition, string[] arguments) ParseJumpExpression(string instruction)
		{
			if (!instruction.StartsWith('<')) Program.Panic();

			instruction = instruction[1..].TrimStart(); // Trim '<'

			if (!instruction.TrimEnd().Contains(' ')) // If jump instruction doesn't contain a condition
				return (instruction.TrimEnd(), null, []);

			string label = instruction[..instruction.IndexOf(' ')]; // Before ' '
			instruction = instruction[instruction.IndexOf(' ')..].TrimStart(); // Trim label

			if (!instruction.Contains('?') || !instruction.Contains(':')) throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction);

			string condition = instruction[(instruction.IndexOf('?') + 1)..instruction.IndexOf(':')].Trim();

			if (instruction.Length > instruction.IndexOf(':') + 1 /* Arguments are passed */ && instruction[instruction.IndexOf(':') + 1] != ' ' /* Arguments are not separated with a space character */)
				throw new InvalidCodeException(currentLine, CodeError.InvalidInstruction, "Condition's arguments must be separated with a space. Insert a space character after ':'.");

			instruction = instruction[(instruction.IndexOf(':') + 1 + 1)..]; // Trim condition

			if (!condition.IsAlphanumericWithSpaces)
				throw new InvalidCodeException(currentLine, CodeError.InvalidConditionName, $"Condition \"{condition}\" violates condition naming rules: only numbers (0-9), Latin letters (A-Z) and spaces are permitted.");

			string[] arguments = ParseArguments(instruction);

			return (label, condition, arguments);
		}

		private string[] ParseArguments(string rawArguments) // "a;b1\;b2;\(var)\\c" where value 'var' equals "test" => ["a", "b1;b2", @"test\c"]
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
					else if (insideEscapeSequence) throw new InvalidCodeException(currentLine, CodeError.EscapeSequenceNotFound, $"Escape sequence \"\\{argument[i]}\" does not exist. Have you made a typo? Are you using the wrong language version (\"{languageVersion}\")?");
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
			if (args.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.EqualsCondition);

			return args[0] == args[1];
		}

		private bool IsTrue_Matches(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.MatchesCondition);
			if (args.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.MatchesCondition);

			try
			{
				return Regex.IsMatch(args[0], args[1]);
			}
			catch (ArgumentException)
			{
				throw new InvalidCodeException(currentLine, CodeError.InvalidArguments, Usage.WithMessage(Usage.MatchesCondition, $"Regular expression (argument 2: \"{args[1]}\") cannot be parsed."));
			}
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
				throw new InvalidCodeException(e.Line, e.Reason, e.Details?.Replace(Usage.MatchesCondition, Usage.NotMatchesCondition));
			}
		}

		#endregion

		#region Commands

		#region set ...

		private void SetValue(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.SetValueCommand);
			if (args.Length != 2) throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.SetValueCommand);

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
			else throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.PrintCommand);
		}

		private void PrintLine(params string[] args)
		{
			if (args.Length == 0) Console.WriteLine();
			else if (args.Length == 1) Console.WriteLine(args[0]);
			else throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.PrintLineCommand);
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
			if (args.Length != 1) throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.AskLineCommand);

			string valueName = args[0];

			string? input = Console.ReadLine();
			if (input is null) Program.Exit();
			else SetValue(valueName, input);
		}

		#endregion

		#region execute ...

		private void Execute(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteCommand);
			if (args.Length != 1) throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.ExecuteCommand);

			string fileName, arguments = string.Empty;

			if (args[0].Trim().Contains(' '))
			{
				int argumentsStartIndex = args[0].Trim().IndexOf(' ') + 1; // Index of the next character after space
				fileName = args[0][..(argumentsStartIndex - 1)]; // Before space
				arguments = args[0][argumentsStartIndex..]; // After space
			}
			else fileName = args[0];

			ProcessStartInfo startInfo = new()
			{
				UseShellExecute = true,
				FileName = fileName,
				Arguments = arguments
			};

			Process.Start(startInfo);
			SetValue("_last_execute_status", string.Empty);
		}

		private void ExecuteWait(params string[] args)
		{
			if (args.Length == 0) throw new InvalidCodeException(currentLine, CodeError.ArgumentsRequired, Usage.ExecuteWaitCommand);
			if (args.Length != 1) throw new InvalidCodeException(currentLine, CodeError.InvalidArgumentCount, Usage.ExecuteWaitCommand);

			string fileName, arguments = string.Empty;

			if (args[0].Trim().Contains(' '))
			{
				int argumentsStartIndex = args[0].Trim().IndexOf(' ') + 1; // Index of the next character after space
				fileName = args[0][..(argumentsStartIndex - 1)]; // Before space
				arguments = args[0][argumentsStartIndex..]; // After space
			}
			else fileName = args[0];

			ProcessStartInfo startInfo = new()
			{
				UseShellExecute = true,
				FileName = fileName,
				Arguments = arguments
			};

			Process process = new() { StartInfo = startInfo };
			process.Start();
			process.WaitForExit();
			SetValue("_last_execute_status", process.ExitCode.ToString());
		}

		#endregion

		#endregion
	}
}
