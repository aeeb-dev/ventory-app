using System.Diagnostics;

namespace Bferal.VentoryDesktop;

internal static class VentoryInstall
{
    /// <summary>Carpeta de instalación por usuario (como otros programas en AppData).</summary>
    public static string InstallDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ventory");

    public static string InstalledExecutable => Path.Combine(InstallDirectory, "Ventory.exe");

    /// <summary>
    /// Si no corre desde la carpeta instalada, copia archivos e inicia la copia instalada.
    /// Devuelve true si este proceso debe terminar.
    /// </summary>
    public static bool TryRelaunchFromInstallIfNeeded(string[] args)
    {
        var current = Path.GetFullPath(Application.ExecutablePath);
        var installed = Path.GetFullPath(InstalledExecutable);

        if (PathsEqual(current, installed))
            return false;

        try
        {
            Directory.CreateDirectory(InstallDirectory);
            CopyDeploymentToInstall(current);
            LaunchInstalled(args);
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo instalar VENTORY en:\n{InstallDirectory}\n\n{ex.Message}",
                "VENTORY",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }
    }

    private static void CopyDeploymentToInstall(string currentExe)
    {
        var sourceDir = Path.GetDirectoryName(currentExe)!;
        var dllCount = Directory.GetFiles(sourceDir, "*.dll").Length;

        if (dllCount > 3)
        {
            CopyDirectory(sourceDir, InstallDirectory, skipPdb: true);
            return;
        }

        CopyFileWithRetry(currentExe, InstalledExecutable);
        var ico = Path.Combine(sourceDir, "icon-inv-favicon.ico");
        if (File.Exists(ico))
            CopyFileWithRetry(ico, Path.Combine(InstallDirectory, "icon-inv-favicon.ico"));

        foreach (var name in new[] { "update.json" })
        {
            var cfg = Path.Combine(sourceDir, name);
            if (File.Exists(cfg))
                CopyFileWithRetry(cfg, Path.Combine(InstallDirectory, name));
        }
    }

    private static void CopyDirectory(string source, string target, bool skipPdb = false)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            if (skipPdb && file.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                continue;
            var relative = Path.GetRelativePath(source, file);
            var dest = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            CopyFileWithRetry(file, dest);
        }
    }

    private static void CopyFileWithRetry(string source, string target)
    {
        const int attempts = 5;
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                if (File.Exists(target))
                {
                    var srcTime = File.GetLastWriteTimeUtc(source);
                    var dstTime = File.GetLastWriteTimeUtc(target);
                    if (srcTime <= dstTime && new FileInfo(source).Length == new FileInfo(target).Length)
                        return;
                }

                File.Copy(source, target, overwrite: true);
                return;
            }
            catch (IOException) when (i < attempts - 1)
            {
                Thread.Sleep(120);
            }
        }
    }

    private static void LaunchInstalled(string[] args)
    {
        var psi = new ProcessStartInfo(InstalledExecutable)
        {
            UseShellExecute = true,
            WorkingDirectory = InstallDirectory,
        };
        if (args.Length > 0)
            psi.Arguments = string.Join(" ", args.Select(QuoteArg));
        Process.Start(psi);
    }

    private static string QuoteArg(string arg) =>
        arg.Contains(' ') ? $"\"{arg}\"" : arg;

    private static bool PathsEqual(string a, string b) =>
        string.Equals(
            Path.GetFullPath(a).TrimEnd('\\'),
            Path.GetFullPath(b).TrimEnd('\\'),
            StringComparison.OrdinalIgnoreCase);
}