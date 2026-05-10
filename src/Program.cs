// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace TsslInterpreter;

internal enum CriticalError
{
	Unknown = 1,
	InvalidArguments,
	FileReadError, EmptyFile,
	NotSupportedLanguageVersion,
	InvalidCode
}

internal static class Program
{
	public const string Name = "TsslInterpreter";
	public const string FriendlyName = "TSSL::Interpreter";
	public const string Website = "https://tssl.yarb00.dev";
	public const string License = """
		Copyright (c) 2025-2026 yarb00

		Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

		The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
		"""; // MIT (Expat)

	public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version ?? throw new UnreachableException("Assembly version can't be null.");
	public static readonly string FriendlyVersion = Version.ToString(3);

	private const string issueReportUrl = $"{Website}/issue/report/client";

	private static readonly string title = $"{FriendlyName} v{FriendlyVersion}";

	private static readonly FrozenDictionary<CriticalError, string> messageByErrorType = new Dictionary<CriticalError, string>
	{
		[CriticalError.InvalidArguments] = "Received arguments are invalid.",
		[CriticalError.FileReadError] = "An error occurred while reading the code file. Does it exist? Is it accessible?",
		[CriticalError.EmptyFile] = "The code file is empty.",
		[CriticalError.NotSupportedLanguageVersion] = "The TSSL version specified in this script is not supported by this version of interpreter.",
		[CriticalError.InvalidCode] = "Your code contains an error."
	}.ToFrozenDictionary();

	public static void Main(string[] args)
	{
		Console.Title = title;
		Console.CancelKeyPress += (_, _) => Exit(shouldKillProcess: false);

		if (!Debugger.IsAttached)
		{
			AppDomain.CurrentDomain.UnhandledException += static (_, e) => HandleUnhandledException((Exception)e.ExceptionObject);
			TaskScheduler.UnobservedTaskException += static (_, e) => HandleUnhandledException(e.Exception);
		}

		if (args.Length != 1)
		{
			PrintUsage();
			Panic(CriticalError.InvalidArguments);
		}

		switch (args[0])
		{
			case "--help" or "-h": PrintUsage(); break;
			case "--license" or "-l": Console.WriteLine(License); break;
			case "--version" or "-v": Console.WriteLine(FriendlyVersion); break;
			case "--check-updates" or "-u": CheckUpdates(); break;

			case { } when args[0].StartsWith("--") || args[0].StartsWith('-'):
				PrintUsage();
				Panic(CriticalError.InvalidArguments, $"Option \"{args[0]}\" does not exist.");
				break;

			default: RunCodeFromFile(args[0]); break;
		}
	}

	public static void Panic(CriticalError error = CriticalError.Unknown, string? message = null)
	{
		if (message is null && !messageByErrorType.TryGetValue(error, out message)) message = "An error occurred.";

		Exit(message, (int)error);
	}

	public static void Exit(string message = "", int exitCode = 0, bool shouldKillProcess = true)
	{
		if (!message.IsEmptyOrWhitespace || exitCode != 0)
		{
			Console.WriteLine();
			Console.WriteLine(new string('=', title.Length));

			if (exitCode == 0) Console.Write($"{FriendlyName} exited");
			else Console.Write($"{FriendlyName} exited with code {exitCode}");

			if (message.IsEmptyOrWhitespace) Console.WriteLine('.');
			else
			{
				Console.WriteLine(':');
				Console.WriteLine(message);
			}
		}

		if (shouldKillProcess) Environment.Exit(exitCode);
	}

	private static void PrintUsage()
	{
		string executableName = Path.GetFileNameWithoutExtension(Environment.ProcessPath) ?? string.Empty;
		if (!executableName.IsEmpty) executableName += " ";

		Console.WriteLine($"""
				Welcome to {title}. Usage:
					Run TSSL code from a file:
						{executableName}<path>
					Print this text:
						{executableName}--help
						{executableName}-h
					Print {FriendlyName} license:
						{executableName}--license
						{executableName}-l
					Print {FriendlyName} version:
						{executableName}--version
						{executableName}-v
					Check for {FriendlyName} updates:
						{executableName}--check-updates
						{executableName}-u

				Visit {Website} for more information.
				""");
	}

	private static void CheckUpdates()
	{
		Console.WriteLine($"Welcome to {title}. Preparing to check for updates.");

		Console.WriteLine(Updater.UpdateDataLocation switch
		{
			Updater.DataLocation.Server => "Update data will be fetched from a remote server. To use a local file, set the 'TSSL_INTERPRETER_LOCAL_UPDATE_DATA_PATH' environment variable.",
			Updater.DataLocation.Local => "Update data will be loaded from a local file. To fetch update data from the server, unset the 'TSSL_INTERPRETER_LOCAL_UPDATE_DATA_PATH' environment variable.",
			_ => throw new UnreachableException("Update data location value is not valid.")
		});

		Console.WriteLine("Getting the update data...");

		UpdateData updateData = Updater.GetUpdateData().GetAwaiter().GetResult();

		// IsUpdateAvailable returns null if update data doesn't contain the version information
		if (Updater.IsUpdateAvailable(updateData) is false) Console.WriteLine("You're using the latest version!");
		else Console.WriteLine($"""
			Version {updateData.LatestVersion?.ToString(3) ?? "[Invalid data]"} is available!

			Information about the update:
			{updateData.DetailsUrl?.ToString() ?? "[Invalid data]"}
			""");
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

		new ScriptEnvironment(codeLines).ExecuteScript();
	}

	private static void HandleUnhandledException(Exception e)
	{
		string
			issueTitle = $"Unhandled exception: `{e.GetType().Name}`",
			issueBody = $"""
				***Auto-generated by {FriendlyName}.***

				## Environment

				**{FriendlyName} version**: `{FriendlyVersion}`

				**OS**: `{RuntimeInformation.OSDescription}`

				## Exception details

				```
				{e}
				```
				""";

		Exit($"""
			An unhandled exception occurred!
			It's a serious error that should not normally happen.

			Please report it by visiting this URL
			(report should be already prefilled with details, but please add more information about what you did that crashed the program):
			{issueReportUrl}?title={Uri.EscapeDataString(issueTitle)}&body={Uri.EscapeDataString(issueBody)}

			If you know what you're doing, here are advanced details:
			{e}
			""", shouldKillProcess: false);
	}
}
