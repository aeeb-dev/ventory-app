using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bferal.VentoryDesktop;

internal sealed record UpdateCheckResult(
    bool Success,
    bool UpdateAvailable,
    string Message,
    Version? RemoteVersion = null,
    string? PortableDownloadUrl = null,
    string? ZipDownloadUrl = null);

internal static class VentoryGitHubUpdate
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(3) };
    }

    public static Version CurrentVersion()
    {
        try
        {
            var info = FileVersionInfo.GetVersionInfo(Application.ExecutablePath);
            if (Version.TryParse(info.FileVersion, out var v)) return v;
        }
        catch
        {
            /* ignore */
        }

        return new Version(1, 0, 0, 0);
    }

    public static async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var config = VentoryUpdateConfig.Load();
        var token = VentoryUpdateConfig.ResolveGitHubToken();
        if (string.IsNullOrEmpty(token))
        {
            return new UpdateCheckResult(
                false,
                false,
                "Falta token de GitHub. Creá el archivo:\n" +
                $"%LocalAppData%\\Ventory\\github.token\n\n" +
                "O la variable de entorno VENTORY_GITHUB_TOKEN (PAT con acceso read al repo privado).");
        }

        var url = $"https://api.github.com/repos/{config.GitHubOwner}/{config.GitHubRepo}/releases/latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("VentoryDesktop-Update");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        HttpResponseMessage response;
        try
        {
            response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(false, false, $"Sin conexión a GitHub: {ex.Message}");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return new UpdateCheckResult(
                false,
                false,
                response.StatusCode switch
                {
                    System.Net.HttpStatusCode.NotFound =>
                        "No hay releases en GitHub. Publicá un tag ventory-vX.Y.Z desde la rama app-local.",
                    System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden =>
                        "Token inválido o sin permiso de lectura en el repo privado.",
                    _ => $"GitHub respondió {(int)response.StatusCode}: {Trim(body, 200)}",
                });
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var remoteVersion = ParseReleaseVersion(tag);
        if (remoteVersion is null)
            return new UpdateCheckResult(false, false, $"No se pudo leer la versión del tag: {tag}");

        string? exeUrl = null;
        string? zipUrl = null;
        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                var download = asset.GetProperty("browser_download_url").GetString();
                if (name.Equals(config.PortableAssetName, StringComparison.OrdinalIgnoreCase))
                    exeUrl = download;
                else if (name.Equals(config.InstallZipAssetName, StringComparison.OrdinalIgnoreCase))
                    zipUrl = download;
            }
        }

        if (exeUrl is null && zipUrl is null)
        {
            return new UpdateCheckResult(
                false,
                false,
                $"El último release ({tag}) no incluye {config.PortableAssetName} ni {config.InstallZipAssetName}.");
        }

        var current = CurrentVersion();
        if (remoteVersion <= current)
        {
            return new UpdateCheckResult(
                true,
                false,
                $"Ya tenés la última versión ({current}). Último release: {tag}.");
        }

        return new UpdateCheckResult(
            true,
            true,
            $"Hay actualización: {current} → {remoteVersion} ({tag}).",
            remoteVersion,
            exeUrl,
            zipUrl);
    }

    public static async Task<string?> DownloadPortableUpdateAsync(
        string downloadUrl,
        CancellationToken cancellationToken = default)
    {
        var token = VentoryUpdateConfig.ResolveGitHubToken();
        if (string.IsNullOrEmpty(token)) return null;

        var tempDir = Path.Combine(Path.GetTempPath(), "Ventory-update", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var target = Path.Combine(tempDir, "Ventory.new.exe");

        using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("VentoryDesktop-Update");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

        using var response = await Http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var file = File.Create(target);
        await stream.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        return target;
    }

    public static void ScheduleApplyPortableUpdateAndRestart(string newExePath)
    {
        var installDir = VentoryInstall.InstallDirectory;
        var installedExe = VentoryInstall.InstalledExecutable;
        Directory.CreateDirectory(installDir);

        var script = Path.Combine(Path.GetTempPath(), $"ventory-apply-update-{Guid.NewGuid():N}.ps1");
        var content = $$"""
            $ErrorActionPreference = 'Stop'
            $src = '{{newExePath.Replace("'", "''")}}'
            $dir = '{{installDir.Replace("'", "''")}}'
            $exe = '{{installedExe.Replace("'", "''")}}'
            Start-Sleep -Seconds 2
            Get-Process Ventory -ErrorAction SilentlyContinue | Stop-Process -Force
            Start-Sleep -Seconds 1
            Copy-Item -LiteralPath $src -Destination $exe -Force
            Start-Process -FilePath $exe
            Remove-Item -LiteralPath '{{script.Replace("'", "''")}}' -Force -ErrorAction SilentlyContinue
            """;
        File.WriteAllText(script, content);

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{script}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        });

        Application.Exit();
    }

    private static Version? ParseReleaseVersion(string tag)
    {
        var cleaned = tag.Trim();
        if (cleaned.StartsWith("ventory-v", StringComparison.OrdinalIgnoreCase))
            cleaned = cleaned["ventory-v".Length..];
        else if (cleaned.StartsWith('v') || cleaned.StartsWith('V'))
            cleaned = cleaned[1..];

        if (Version.TryParse(Regex.Replace(cleaned, "^[^0-9]+", ""), out var direct))
            return direct;

        return Version.TryParse(cleaned, out var fallback) ? fallback : null;
    }

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}