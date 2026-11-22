using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bferal.VentoryDesktop;

internal sealed class VentoryUpdateConfig
{
    [JsonPropertyName("githubOwner")]
    public string GitHubOwner { get; set; } = "aeeb-dev";

    [JsonPropertyName("githubRepo")]
    public string GitHubRepo { get; set; } = "ventory-app";

    [JsonPropertyName("portableAssetName")]
    public string PortableAssetName { get; set; } = "Ventory.exe";

    [JsonPropertyName("installZipAssetName")]
    public string InstallZipAssetName { get; set; } = "Ventory-desktop.zip";

    public static VentoryUpdateConfig Load()
    {
        var defaults = new VentoryUpdateConfig();
        foreach (var path in ConfigPaths())
        {
            try
            {
                if (!File.Exists(path)) continue;
                var json = File.ReadAllText(path);
                var fromFile = JsonSerializer.Deserialize<VentoryUpdateConfig>(json, JsonOptions);
                if (fromFile is null) continue;
                return Merge(defaults, fromFile);
            }
            catch
            {
                /* siguiente ruta */
            }
        }

        return defaults;
    }

    public static string? ResolveGitHubToken()
    {
        var fromEnv = Environment.GetEnvironmentVariable("VENTORY_GITHUB_TOKEN")?.Trim();
        if (!string.IsNullOrEmpty(fromEnv)) return fromEnv;

        foreach (var path in TokenPaths())
        {
            try
            {
                if (!File.Exists(path)) continue;
                var line = File.ReadAllLines(path).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
                if (!string.IsNullOrEmpty(line)) return line;
            }
            catch
            {
                /* ignore */
            }
        }

        return VentoryBuildSecrets.EmbeddedGitHubToken;
    }

    private static IEnumerable<string> ConfigPaths()
    {
        yield return Path.Combine(VentoryInstall.InstallDirectory, "update.json");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VentoryDesktop",
            "update.json");
    }

    private static IEnumerable<string> TokenPaths()
    {
        yield return Path.Combine(VentoryInstall.InstallDirectory, "github.token");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VentoryDesktop",
            "github.token");
    }

    private static VentoryUpdateConfig Merge(VentoryUpdateConfig defaults, VentoryUpdateConfig file) =>
        new()
        {
            GitHubOwner = string.IsNullOrWhiteSpace(file.GitHubOwner) ? defaults.GitHubOwner : file.GitHubOwner.Trim(),
            GitHubRepo = string.IsNullOrWhiteSpace(file.GitHubRepo) ? defaults.GitHubRepo : file.GitHubRepo.Trim(),
            PortableAssetName = string.IsNullOrWhiteSpace(file.PortableAssetName)
                ? defaults.PortableAssetName
                : file.PortableAssetName.Trim(),
            InstallZipAssetName = string.IsNullOrWhiteSpace(file.InstallZipAssetName)
                ? defaults.InstallZipAssetName
                : file.InstallZipAssetName.Trim(),
        };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}