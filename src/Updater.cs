// https://tssl.yarb00.dev

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace TsslInterpreter;

internal readonly record struct UpdateData(Version? LatestVersion, Uri? DetailsUrl);

internal static class Updater
{
	private const string updateDataUrl = $"{Program.Website}/update/data/client/{updateChannel}.tssl-update-data.json";
	private const string updateChannel = "release";

	public static bool IsUpdateAvailable(UpdateData updateData)
	{
		if (updateData.LatestVersion is null || Program.Version is null) Program.Panic(message: "Can't check updates: version is missing.");

		return IsUpdateAvailable(updateData.LatestVersion!, Program.Version!);
	}

	public static bool IsUpdateAvailable(Version latestVersion, Version installedVersion)
	{
		Version
			latestVersionTrimmed = new(latestVersion.Major, latestVersion.Minor, latestVersion.Build),
			installedVersionTrimmed = new(installedVersion.Major, installedVersion.Minor, installedVersion.Build);

		return latestVersionTrimmed > installedVersionTrimmed;
	}

	public static async Task<UpdateData> GetUpdateData()
	{
		using HttpClient httpClient = new();
		httpClient.DefaultRequestHeaders.UserAgent.Add(new(Program.Name, Program.Version?.ToString(3)));

		string response;
		try
		{
			response = await httpClient.GetStringAsync(updateDataUrl);
		}
		catch
		{
			Program.Panic(message: "An error occurred while fetching the update data.");
			throw;
		}

		JsonElement updateData;
		try
		{
			updateData = JsonDocument.Parse(response, new JsonDocumentOptions
			{
				AllowTrailingCommas = true,
				CommentHandling = JsonCommentHandling.Skip
			}).RootElement;
		}
		catch
		{
			Program.Panic(message: "The update data server have sent are not valid (can't parse JSON).");
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
