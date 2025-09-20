// https://yarb00.dev

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TsslInterpreter;

internal enum CriticalError
{
	WrongArguments,
	WrongCode,
	FileReadError,
	EmptyFile,
	WrongVersion
}

internal static class Program
{
	private readonly static Dictionary<CriticalError, (string errorMessage, int exitCode)> errorDataByErrorType = new()
	{
		[CriticalError.WrongArguments] = ("You should pass exactly 1 argument: the path to the code file.", 2),
		[CriticalError.WrongCode] = ("Your code contains an error.", 3),
		[CriticalError.FileReadError] = ("An error occurred while reading the code file. (does it exist? is it accessible?)", 4),
		[CriticalError.EmptyFile] = ("The code file is empty.", 5),
		[CriticalError.WrongVersion] = ("The TSSL version specified in this script is not supported by this version of interpreter.", 6)
	};

	public readonly static Version? Version = Assembly.GetExecutingAssembly().GetName().Version;
	private readonly static string title = $"TsslInterpreter{(Version is not null ? $" v{Version.ToString(3)}" : string.Empty)}";

	public static void Main(string[] args)
	{
		Console.Title = title;
		Console.WriteLine(title);
		Console.WriteLine("https://yarb00.dev");
		Console.WriteLine(new string('=', title.Length));

		AppDomain.CurrentDomain.ProcessExit += (s, e) => Exit(shouldKillProcess: false);
		Console.CancelKeyPress += (s, e) => Exit(shouldKillProcess: false);

		if (args.Length != 1) Panic(CriticalError.WrongArguments);
		RunCodeFromFile(args[0]);
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
			Panic(CriticalError.FileReadError);
			return;
		}

		if (codeLines.Length == 0) Panic(CriticalError.EmptyFile);

		CodeEnvironment environment = new();
		foreach (string codeLine in codeLines) environment.RunInstruction(codeLine);
	}

	public static void Panic(CriticalError? errorType = null)
	{
		if (errorType is null || !errorDataByErrorType.TryGetValue((CriticalError)errorType, out (string message, int exitCode) error))
			Exit("An unknown error occurred.", 1);
		else Exit(error.message, error.exitCode);
	}

	public static void Exit(string message = "Bye.", int exitCode = 0, bool shouldKillProcess = true)
	{
		Console.WriteLine(new string('=', title.Length));
		Console.WriteLine(message);
		if (shouldKillProcess) Environment.Exit(exitCode);
	}
}
