// https://yarb00.dev

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace InterpreterTest;

internal static class Program
{
	private enum CriticalError
	{
		WrongArguments,
		WrongCode,
		FileReadError,
		EmptyFile
	}

	private readonly static Dictionary<CriticalError, (string errorMessage, int exitCode)> errorDataByErrorType = new()
	{
		[CriticalError.WrongArguments] = ("You should pass exactly 1 argument: the path to the code file.", 2),
		[CriticalError.WrongCode] = ("Your code contains an error.", 3),
		[CriticalError.FileReadError] = ("An error occurred while reading the code file. (does it exist? is it accessible?)", 4),
		[CriticalError.EmptyFile] = ("The code file is empty.", 5)
	};

	private static readonly Dictionary<string, string> getValueByName = [];

	public readonly static Version? Version = Assembly.GetExecutingAssembly().GetName().Version;
	private readonly static string title = $"InterpreterTest{(Version is not null ? $" v{Version.ToString(3)}" : string.Empty)}";

	public static void Main(string[] args)
	{
		Console.Title = title;
		Console.WriteLine(title);
		Console.WriteLine("https://yarb00.dev");
		Console.WriteLine(new string('=', title.Length));

		AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit(shouldKillProcess: false);
		Console.CancelKeyPress += (s, e) => Exit(shouldKillProcess: false);

		Console.WriteLine("Welcome.");

		if (args.Length != 1) Panic(CriticalError.WrongArguments);

		Console.WriteLine($"Running your code (from file: \"{args[0]}\").");
		Console.WriteLine(new string('=', title.Length));
		try
		{
			RunCodeFromFile(args[0]);
		}
		catch
		{
			Panic(CriticalError.FileReadError);
		}
	}

	private static void RunCodeFromFile(string filePath)
	{
		string[] codeLines;

		try
		{
			codeLines = File.ReadAllLines(filePath);
		}
		catch
		{
			throw;
		}

		if (codeLines.Length == 0) Panic(CriticalError.EmptyFile);
		if (codeLines[0] != "!InterpreterTestLanguage") Panic(CriticalError.WrongCode);
		codeLines = codeLines[1..];

		foreach (string codeLine in codeLines) RunInstruction(codeLine);
	}

	private static void RunInstruction(string instruction)
	{
		static void Error() => Panic(CriticalError.WrongCode);

		if (string.IsNullOrWhiteSpace(instruction) || instruction.StartsWith('#')) return;

		#region Console output

		else if (instruction.StartsWith("print line>"))
		{
			if (instruction.StartsWith("print line> ")) Console.WriteLine(instruction["print line> ".Length..]);
			else Console.WriteLine();
		}
		else if (instruction.StartsWith("print string>"))
		{
			if (instruction.StartsWith("print string> ")) Console.Write(instruction["print string> ".Length..]);
			else Console.Write(' ');
		}

		else if (instruction.StartsWith("print line value>"))
		{
			if (!instruction.StartsWith("print line value> ")) Error();

			string
				valueName = instruction["print line value> ".Length..],
				value = getValueByName[valueName];

			Console.WriteLine(value);
		}
		else if (instruction.StartsWith("print string value>"))
		{
			if (!instruction.StartsWith("print string value> ")) Error();

			string
				valueName = instruction["print string value> ".Length..],
				value = getValueByName[valueName];

			Console.Write(value);
		}

		#endregion

		#region Console input

		else if (instruction.StartsWith("ask line>"))
		{
			if (!instruction.StartsWith("ask line> ")) Error();
			string valueName = instruction["ask line> ".Length..];
			string? input = Console.ReadLine();
			if (input is null) Exit();
			else getValueByName[valueName] = input;
		}

		#endregion

		else Error();
	}

	private static void Panic(CriticalError? errorType = null)
	{
		if (errorType is null || !errorDataByErrorType.TryGetValue((CriticalError)errorType, out (string message, int exitCode) error))
			Exit("An unknown error occurred.", 1);
		else Exit(error.message, error.exitCode);
	}

	private static void Exit(string message = "Bye.", int exitCode = 0, bool shouldKillProcess = true)
	{
		Console.WriteLine(new string('=', title.Length));
		Console.WriteLine(message);
		if (shouldKillProcess) Environment.Exit(exitCode);
	}
}
