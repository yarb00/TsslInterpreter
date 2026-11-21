// SPDX-FileCopyrightText: yarb00 <https://www.yarb00.dev>
// SPDX-License-Identifier: MPL-2.0

// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TsslInterpreter;

internal enum UpdateDataLocation
{
	Local,
	Server,
}

internal readonly record struct UpdateData(ProgramVersion LatestVersion, Uri DetailsUrl)
{
	public bool IsUpdateAvailable => LatestVersion.ToVersion() > ProgramVersion.NormalizeVersion(Program.Version);
}

internal readonly record struct ProgramVersion(ulong Major, ulong Minor, ulong Patch)
{
	public static Version NormalizeVersion(Version version) => FromVersion(version).ToVersion();

	public static ProgramVersion FromVersion(Version version) => new()
	{
		Major = (ulong)version.Major,
		Minor = (ulong)version.Minor,
		Patch = version.Build == -1 ? 0 : (ulong)version.Build,
	};

	public readonly Version ToVersion() => checked(new(major: (int)Major, minor: (int)Minor, build: (int)Patch));

	public override readonly string ToString() => $"{Major}.{Minor}.{Patch}";
}

internal static class Updater
{
	public static readonly UpdateDataLocation UpdateDataLocation;

	private const string updateDataServerPath = $"{Program.Website}/update/data/client/release.tssl-update-data.json";

	private static readonly string updateDataLocalPath;

	static Updater()
	{
		updateDataLocalPath = Environment.GetEnvironmentVariable("TSSL_INTERPRETER_LOCAL_UPDATE_DATA_PATH") ?? string.Empty;
		UpdateDataLocation = updateDataLocalPath.IsEmptyOrWhitespace ? UpdateDataLocation.Server : UpdateDataLocation.Local;
	}

	public static async Task<UpdateData> GetUpdateDataAsync() => ParseUpdateData(await GetRawUpdateDataAsync());

	private static async Task<string> GetRawUpdateDataAsync()
	{
		switch (UpdateDataLocation)
		{
			case UpdateDataLocation.Server:
				{
					using HttpClient httpClient = new(); // Fine because the interpreter fetches the update data, then prints it and immediately exits.
					httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));
					httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"{Program.Name}/{Program.FriendlyVersion} (+{Program.Website})");
					try
					{
						return await httpClient.GetStringAsync(updateDataServerPath);
					}
					catch
					{
						Program.Panic(message: $"An error occurred while fetching the update data from the server (\"{updateDataServerPath}\").");
						throw;
					}
				}

			case UpdateDataLocation.Local:
				try
				{
					return await File.ReadAllTextAsync(updateDataLocalPath, Encoding.UTF8);
				}
				catch
				{
					Program.Panic(message: $"An error occurred while reading the update data from file (\"{updateDataLocalPath}\").");
					throw;
				}

			default: throw new UnreachableException("Update data location is invalid.");
		}
	}

	private static UpdateData ParseUpdateData(string rawUpdateData)
	{
		JsonElement updateData;
		try
		{
			updateData = JsonElement.Parse(rawUpdateData, new JsonDocumentOptions
			{
				AllowDuplicateProperties = false,
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip,
			});
		}
		catch
		{
			Program.Panic(message: "The update data is invalid (an error occurred while parsing JSON).");
			throw;
		}

		if (!updateData.TryGetProperty("latest_branch_version", out JsonElement latestVersionElement))
			Program.Panic(message: "The update data is invalid (JSON property \"latest_branch_version\" is not found).");

		if (!updateData.TryGetProperty("latest_branch_version_info", out JsonElement detailsUrlElement))
			Program.Panic(message: "The update data is invalid (JSON property \"latest_branch_version_info\" is not found).");

		ProgramVersion latestVersion;
		try
		{
			latestVersion = ProgramVersion.FromVersion(Version.Parse(latestVersionElement.GetString() ?? throw new Exception()));
		}
		catch
		{
			Program.Panic(message: "The update data is invalid (JSON property \"latest_branch_version\" is not a valid version).");
			throw;
		}

		Uri detailsUrl;
		try
		{
			detailsUrl = new Uri(detailsUrlElement.GetString() ?? throw new Exception());
		}
		catch
		{
			Program.Panic(message: "The update data is invalid (JSON property \"latest_branch_version_info\" is not a valid URL).");
			throw;
		}

		return new(latestVersion, detailsUrl);
	}
}
