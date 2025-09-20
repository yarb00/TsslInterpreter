// https://yarb00.dev

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TsslInterpreter;

internal sealed class CodeEnvironment
{
	private readonly Dictionary<string, string> valueByName = [];
	private string? languageVersion = null;

	public void RunInstruction(string instruction)
	{
		if (instruction.IsNullOrWhiteSpace() || instruction.StartsWith('#')) return;
		if (instruction.StartsWith("!TooSimpleScriptingLanguage "))
		{
			if (languageVersion is not null) Error();
			else
			{
				languageVersion = instruction["!TooSimpleScriptingLanguage ".Length..].Trim();
				return;
			}
		}
		if (languageVersion is null) Error();
		if (languageVersion != "0.2") Panic(CriticalError.WrongVersion);

		// The language version should be specified once with this instruction:
		// "!TooSimpleScriptingLanguage X.Y"
		// where X is major version and Y is minor version
		// before any other non-comment instruction is present.

		string commandName = string.Empty, commandArguments = string.Empty;

		int characterIndex;
		for (characterIndex = 0; characterIndex < instruction.Length; characterIndex++)
		{
			if (instruction[characterIndex] == '>') break;
			if (instruction.Length - 1 == characterIndex) Error();
			else commandName += instruction[characterIndex];
		}

		if (characterIndex + 1 != instruction.Length - 1 && instruction[characterIndex + 1] == ' ') commandArguments = instruction[(characterIndex + 1 + 1)..];
		else if (characterIndex + 1 != instruction.Length - 1 && instruction[characterIndex + 1] != ' ') Error();

		switch (commandName)
		{
			case "print": Print(commandArguments); break;
			case "print value": PrintValue(commandArguments); break;
			case "print line": PrintLine(commandArguments); break;
			case "print line value": PrintLineValue(commandArguments); break;
			case "ask line value": AskLineValue(commandArguments); break;
			case "execute process": ExecuteProcess(commandArguments); break;
			case "execute process wait": ExecuteProcessWait(commandArguments); break;
			default: Error(); break;
		}
	}

	private static void Panic(CriticalError error) => Program.Panic(error);
	private static void Error() => Panic(CriticalError.WrongCode);
	private static void Exit() => Program.Exit();

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
