namespace Bferal.VentoryDesktop;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (VentoryInstall.TryRelaunchFromInstallIfNeeded(args))
            return;

        VentoryUpdateConfig.RefreshInstallUpdateJsonIfNeeded();

        var bootHidden = args.Any(static arg =>
            arg.Equals("--tray-only", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--startup", StringComparison.OrdinalIgnoreCase));

        ApplicationConfiguration.Initialize();
        Application.Run(new VentoryMainForm(bootHidden));
    }
}