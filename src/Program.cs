// https://tssl.yarb00.dev

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TsslInterpreter;

internal enum ProgramError
{
	Unknown = 1,
	InvalidCliArguments,
	ScriptFileReadingFailed,
	EmptyScriptFile,
	UnknownLanguageVersion,
	DeprecatedLanguageVersion,
	InvalidScript,
}

internal static class Program
{
	public const string Name = "TsslInterpreter";

	public const string FriendlyName = "TSSL::Interpreter";

	public const string Website = "https://tssl.yarb00.dev";

	public const string LicenseName = "MIT (Expat)";

	public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version
		?? throw new UnreachableException("Assembly version cannot be null.");

	public static readonly string FriendlyVersion = Version.ToString(3);

	public static readonly string FullVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
		?? throw new UnreachableException("Assembly informational version cannot be null.");

	private const string issueReportBaseUrl = $"{Website}/issue/report/client";

	private static readonly string title = $"{FriendlyName} v{FriendlyVersion}";

	private static readonly FrozenDictionary<ProgramError, string> messageByError = new Dictionary<ProgramError, string>
	{
		[ProgramError.Unknown] = "An error occurred.",
		[ProgramError.InvalidCliArguments] = "Received arguments are invalid.",
		[ProgramError.ScriptFileReadingFailed] = "An error occurred while reading the script file.",
		[ProgramError.EmptyScriptFile] = "The script file is empty.",
		[ProgramError.UnknownLanguageVersion] = "The language version specified in the script is not supported by this version of interpreter.",
		[ProgramError.DeprecatedLanguageVersion] = "The language version specified in the script is no longer supported.",
	}.ToFrozenDictionary();

	public static async Task Main(string[] args)
	{
		Console.InputEncoding = Console.OutputEncoding = Encoding.UTF8;
		Console.Title = title;

		if (!Debugger.IsAttached)
		{
			AppDomain.CurrentDomain.UnhandledException += static (_, e) => ReportUnhandledException((Exception)e.ExceptionObject);
			TaskScheduler.UnobservedTaskException += static (_, e) => ReportUnhandledException(e.Exception);
		}

		if (args.Length != 1)
		{
			PrintUsage();
			Panic(ProgramError.InvalidCliArguments);
		}

		switch (args[0])
		{
			case "--help" or "-h": PrintUsage(); break;

			case "--check-updates" or "-u": await CheckForUpdatesAsync(); break;

			case "--version" or "-v": Console.WriteLine(FullVersion); break;

			case ['-', ..]:
				PrintUsage();
				Panic(ProgramError.InvalidCliArguments, $"Option \"{args[0]}\" does not exist.");
				break;

			default: await ExecuteScriptFromFileAsync(args[0]); break;
		}
	}

	public static void Panic(ProgramError error = ProgramError.Unknown, string? message = null)
	{
		if (message is null && !messageByError.TryGetValue(error, out message)) message = messageByError[ProgramError.Unknown];

		Exit(message, exitCode: (int)error);
	}

	public static void Exit(string? message = null, int? exitCode = 0)
	{
		if (message is not null || exitCode is not (0 or null))
		{
			Console.WriteLine();
			Console.WriteLine(new string('=', title.Length));

			if (exitCode is 0) Console.Write($"{FriendlyName} exited");
			else Console.Write($"{FriendlyName} exited with code {exitCode}");

			if (message is null) Console.WriteLine('.');
			else
			{
				Console.WriteLine(':');
				Console.WriteLine(message);
			}
		}

		if (exitCode is not null) Environment.Exit(exitCode.Value);
	}

	private static void PrintUsage()
	{
		string prefix = Environment.GetCommandLineArgs()[0];

		if (!prefix.IsEmpty) prefix += " ";

		Console.WriteLine($"""
			 _____
			|_   _|
			  | |  <  <  |
			  |_|   >  > |__
			[ TOO SIMPLE SCRIPTING LANGUAGE ]

			Welcome to {title}. Usage:
				Run a script file (*.toosimple):
					{prefix}<path>
				Print this text (usage):
					{prefix}--help
					{prefix}-h
				Check for updates (THIS OPTION WILL CONTACT THE UPDATE SERVER BY DEFAULT!):
					{prefix}--check-updates
					{prefix}-u
				Print the version of {FriendlyName}:
					{prefix}--version
					{prefix}-v

			Visit {Website} for more information.
			""");
	}

	private static async Task CheckForUpdatesAsync()
	{
		Console.WriteLine($"Welcome to {title}. Preparing to check for updates.");

		Console.WriteLine(Updater.UpdateDataLocation switch
		{
			UpdateDataLocation.Server => "Update data will be fetched from a remote server. To use a local file, set the \"TSSL_INTERPRETER_LOCAL_UPDATE_DATA_PATH\" environment variable to the path of the update data file.",
			UpdateDataLocation.Local => "Update data will be loaded from a local file. To fetch update data from the server, unset the \"TSSL_INTERPRETER_LOCAL_UPDATE_DATA_PATH\" environment variable.",
			_ => throw new UnreachableException("Update data location is invalid."),
		});

		Console.WriteLine("Getting the update data...");

		UpdateData updateData = await Updater.GetUpdateDataAsync();

		if (!updateData.IsUpdateAvailable) Console.WriteLine("You're using the latest version!");
		else Console.WriteLine($"""
			Version {updateData.LatestVersion} is available!

			Information about the update:
			{updateData.DetailsUrl}
			""");
	}

	private static async Task ExecuteScriptFromFileAsync(string filePath)
	{
		string[] codeLines;
		try
		{
			codeLines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
		}
		catch
		{
			Panic(ProgramError.ScriptFileReadingFailed);
			return;
		}

		if (codeLines.Length == 0) Panic(ProgramError.EmptyScriptFile);

		new ScriptEnvironment(codeLines).ExecuteScript();
	}

	private static void ReportUnhandledException(Exception e)
	{
		string
			issueTitle = $"Unhandled exception: `{e.GetType().Name}`",
			issueBody = $"""
				***Auto-generated by {FriendlyName}.***

				## Environment

				**{FriendlyName} version**: `{FullVersion}`

				**OS**: `{RuntimeInformation.OSDescription}`

				## Exception details

				```
				{e}
				```
				""",
			issueReportUrl = $"{issueReportBaseUrl}?title={Uri.EscapeDataString(issueTitle)}&body={Uri.EscapeDataString(issueBody)}";

		Exit($"""
			An unhandled exception occurred! x_x
			It's a serious error that should not normally happen.

			Below is a link that will take you to the official GitHub repository, with Issue's title and body partially prefilled with crash details.
			Please add more information about what you did that caused {FriendlyName} to crash.

			{issueReportUrl}

			If you know what you're doing, here are advanced details:
			{e}
			""", exitCode: null);
	}
}
