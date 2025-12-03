// https://tssl.yarb00.dev

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TsslInterpreter;

internal readonly record struct UpdateData(Version? LatestVersion, Uri? DetailsUrl);

internal static class Updater
{
	public enum DataLocation
	{
		Server,
		Local
	}

	public static readonly DataLocation UpdateDataLocation;

	private const string updateDataServerPath = $"{Program.Website}/update/data/client/release.tssl-update-data.json";
	private static readonly string updateDataLocalPath = string.Empty;

	static Updater()
	{
		string localUpdateDataPath = Environment.GetEnvironmentVariable("TSSL_INTERPRETER_LOCAL_UPDATE_DATA_PATH") ?? string.Empty;

		if (localUpdateDataPath.IsEmptyOrWhitespace) UpdateDataLocation = DataLocation.Server;
		else
		{
			UpdateDataLocation = DataLocation.Local;
			updateDataLocalPath = localUpdateDataPath;
		}
	}

	public static bool? IsUpdateAvailable(UpdateData updateData) => updateData.LatestVersion is null ? null : IsUpdateAvailable(updateData.LatestVersion, Program.Version);

	public static bool IsUpdateAvailable(Version latestVersion, Version installedVersion) =>
		// Trim the 4th section of Version, since TsslInterpreter versions are in the A.B.C format
		new Version(latestVersion.Major, latestVersion.Minor, latestVersion.Build) > new Version(installedVersion.Major, installedVersion.Minor, installedVersion.Build);

	public static async Task<UpdateData> GetUpdateData()
	{
		string rawUpdateData;

		switch (UpdateDataLocation)
		{
			case DataLocation.Server:
				{
					using HttpClient httpClient = new();
					httpClient.DefaultRequestHeaders.UserAgent.Add(new(Program.Name, Program.FriendlyVersion));
					try
					{
						rawUpdateData = await httpClient.GetStringAsync(updateDataServerPath);
					}
					catch
					{
						Program.Panic(message: $"An error occurred while fetching the update data from the server (\"{updateDataServerPath}\").");
						throw;
					}
					break;
				}

			case DataLocation.Local:
				try
				{
					rawUpdateData = File.ReadAllText(updateDataLocalPath, Encoding.UTF8);
				}
				catch
				{
					Program.Panic(message: $"An error occurred while reading the file with the update data (\"{updateDataLocalPath}\").");
					throw;
				}
				break;

			default: throw new UnreachableException("Update data location value is not valid.");
		}

		JsonElement updateData;
		try
		{
			updateData = JsonDocument.Parse(rawUpdateData, new JsonDocumentOptions
			{
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip
			}).RootElement;
		}
		catch
		{
			Program.Panic(message: "The update data is not valid (can't parse JSON).");
			throw;
		}

		return new UpdateData
		{
			LatestVersion = GetLatestVersion(updateData),
			DetailsUrl = GetDetailsUrl(updateData)
		};
	}

	private static Version? GetLatestVersion(JsonElement updateData)
	{
		if (!updateData.TryGetProperty("latest_branch_version", out JsonElement latestBranchVersionElement)) return null;

		string? rawLatestVersion = latestBranchVersionElement.GetString();

		_ = Version.TryParse(rawLatestVersion, out Version? latestVersion);

		return latestVersion;
	}

	private static Uri? GetDetailsUrl(JsonElement updateData)
	{
		if (!updateData.TryGetProperty("latest_branch_version_info", out JsonElement latestBranchVersionInfoElement)) return null;

		string? rawDetailsUrl = latestBranchVersionInfoElement.GetString();

		_ = Uri.TryCreate(rawDetailsUrl, new UriCreationOptions(), out Uri? detailsUrl);

		return detailsUrl;
	}
}
