using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Win32;

namespace Bferal.VentoryDesktop;

internal sealed class VentoryMainForm : Form
{
    private const string PrimaryUrl = "https://ventory.bferal.cl";
    private const string FallbackUrl = "https://ventory.tasker.cl";
    private const string RunKeyName = "Ventory";
    private const string LegacyRunKeyName = "VentoryDesktop";

    private static readonly string NotificationHookScript = """
        (function () {
          if (window.__ventoryDesktopHooked) return;
          window.__ventoryDesktopHooked = true;
          const NativeEventSource = window.EventSource;
          if (!NativeEventSource || !window.chrome?.webview) return;
          window.EventSource = function VentoryEventSource(url, options) {
            const source = new NativeEventSource(url, options);
            source.addEventListener("message", (event) => {
              if (!event.data || event.data === ": heartbeat") return;
              try {
                const data = JSON.parse(event.data);
                if (data.type === "NOTIFICATION") {
                  window.chrome.webview.postMessage(
                    JSON.stringify({ type: "NOTIFICATION", payload: data.payload || {} })
                  );
                }
              } catch (_) {}
            });
            return source;
          };
          window.EventSource.prototype = NativeEventSource.prototype;
          window.EventSource.CONNECTING = NativeEventSource.CONNECTING;
          window.EventSource.OPEN = NativeEventSource.OPEN;
          window.EventSource.CLOSED = NativeEventSource.CLOSED;
        })();
        """;

    private readonly bool _startHidden;
    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly NotifyIcon _tray;
    private readonly HashSet<string> _seenNotificationIds = new(StringComparer.Ordinal);
    private readonly System.Windows.Forms.Timer _updatePollTimer;
    private string _activeUrl;
    private bool _updatePromptOpen;

    public VentoryMainForm(bool startHidden)
    {
        _startHidden = startHidden;
        _activeUrl = LoadActiveUrl();

        Text = BuildWindowTitle();
        Width = 1320;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 600);
        var appIcon = LoadAppIcon();
        Icon = (Icon)appIcon.Clone();

        _tray = CreateTrayIcon(appIcon);
        _updatePollTimer = new System.Windows.Forms.Timer { Interval = (int)TimeSpan.FromHours(2).TotalMilliseconds };
        _updatePollTimer.Tick += (_, _) => _ = CheckForUpdatesSilentAsync();
        Controls.Add(_webView);

        Load += OnFormLoadAsync;
        FormClosing += OnFormClosing;
        if (_startHidden)
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Visible = false;
        }
    }

    private async void OnFormLoadAsync(object? sender, EventArgs e)
    {
        LoadSeenNotificationIds();
        await InitWebViewAsync();
        if (_startHidden)
            HideToTray();
        else
            ShowFromTray();

        _updatePollTimer.Start();

        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            await CheckForUpdatesSilentAsync().ConfigureAwait(false);
        });
    }

    private async Task CheckForUpdatesSilentAsync()
    {
        var result = await VentoryGitHubUpdate.CheckForUpdatesAsync().ConfigureAwait(true);
        if (!result.Success || !result.UpdateAvailable) return;

        if (InvokeRequired)
        {
            BeginInvoke(() => _ = PromptForcedUpdateIfNeededAsync(result));
            return;
        }

        await PromptForcedUpdateIfNeededAsync(result).ConfigureAwait(true);
    }

    private async Task PromptForcedUpdateIfNeededAsync(UpdateCheckResult result)
    {
        if (_updatePromptOpen) return;
        _updatePromptOpen = true;
        try
        {
            await OfferAndApplyUpdateAsync(result, forcedPrompt: true).ConfigureAwait(true);
        }
        finally
        {
            _updatePromptOpen = false;
        }
    }

    private async Task CheckForUpdatesInteractiveAsync()
    {
        try
        {
            var result = await VentoryGitHubUpdate.CheckForUpdatesAsync().ConfigureAwait(true);
            if (!result.Success)
            {
                MessageBox.Show(this, result.Message, "VENTORY — actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!result.UpdateAvailable)
            {
                MessageBox.Show(this, result.Message, "VENTORY — actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await OfferAndApplyUpdateAsync(result, forcedPrompt: false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            UseWaitCursor = false;
            MessageBox.Show(this, ex.Message, "VENTORY — actualizaciones", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OfferAndApplyUpdateAsync(UpdateCheckResult result, bool forcedPrompt)
    {
        if (forcedPrompt)
        {
            var current = VentoryGitHubUpdate.CurrentVersion();
            var remote = result.RemoteVersion?.ToString() ?? "nueva";
            var msg =
                $"Hay una nueva versión ({current} → {remote}).\n\n" +
                "Es necesario actualizar para seguir usando VENTORY.\n" +
                "Se descargará e instalará ahora; la aplicación se reiniciará.";
            if (!ShowActualizarAhoraDialog(msg, "VENTORY — actualización requerida"))
                return;
        }
        else
        {
            var confirm = MessageBox.Show(
                this,
                result.Message + "\n\n¿Descargar e instalar ahora? La app se reiniciará.",
                "VENTORY — actualización",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
        }

        await DownloadAndApplyUpdateAsync(result).ConfigureAwait(true);
    }

    private async Task DownloadAndApplyUpdateAsync(UpdateCheckResult result)
    {
        if (string.IsNullOrEmpty(result.PortableDownloadUrl))
        {
            MessageBox.Show(this, "Este release no incluye Ventory.exe.", "VENTORY", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UseWaitCursor = true;
        try
        {
            var path = await VentoryGitHubUpdate
                .DownloadPortableUpdateAsync(result.PortableDownloadUrl)
                .ConfigureAwait(true);

            if (path is null)
            {
                MessageBox.Show(this, "No se pudo descargar la actualización.", "VENTORY", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            VentoryGitHubUpdate.ScheduleApplyPortableUpdateAndRestart(path);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static string BuildWindowTitle()
    {
        var v = VentoryGitHubUpdate.CurrentVersion();
        var label = v.Revision > 0 ? $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}" : $"{v.Major}.{v.Minor}.{v.Build}";
        return $"VENTORY | v{label}";
    }

    private bool ShowActualizarAhoraDialog(string message, string title)
    {
        using var dialog = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ControlBox = false,
            ShowInTaskbar = true,
            ClientSize = new Size(440, 168),
            Icon = Icon,
        };

        var label = new Label
        {
            Text = message,
            AutoSize = false,
            Bounds = new Rectangle(16, 16, 408, 88),
        };
        var button = new Button
        {
            Text = "Actualizar ahora",
            DialogResult = DialogResult.OK,
            Bounds = new Rectangle(144, 112, 152, 32),
        };
        dialog.Controls.Add(label);
        dialog.Controls.Add(button);
        dialog.AcceptButton = button;

        return dialog.ShowDialog(this) == DialogResult.OK;
    }

    private async Task InitWebViewAsync()
    {
        var userData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VentoryDesktop",
            "WebView2");
        Directory.CreateDirectory(userData);

        var env = await CoreWebView2Environment.CreateAsync(userDataFolder: userData);
        await _webView.EnsureCoreWebView2Async(env);

        var core = _webView.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            Process.Start(new ProcessStartInfo(args.Uri) { UseShellExecute = true });
        };
        core.NavigationCompleted += (_, _) => _ = core.ExecuteScriptAsync(NotificationHookScript);
        core.WebMessageReceived += OnWebMessageReceived;

        core.Navigate(_activeUrl);
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var json = e.TryGetWebMessageAsString();
            if (string.IsNullOrEmpty(json)) return;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.GetProperty("type").GetString() != "NOTIFICATION") return;
            var payload = root.GetProperty("payload");
            var id = payload.TryGetProperty("id", out var idEl)
                ? idEl.GetString() ?? ""
                : "";
            var message = payload.TryGetProperty("message", out var msgEl)
                ? msgEl.GetString() ?? "Nueva notificación"
                : "Nueva notificación";
            if (!string.IsNullOrEmpty(id))
            {
                if (_seenNotificationIds.Contains(id)) return;
                _seenNotificationIds.Add(id);
                SaveSeenNotificationIds();
            }
            _tray.ShowBalloonTip(4000, "VENTORY", message, ToolTipIcon.Info);
        }
        catch
        {
            /* ignore malformed messages */
        }
    }

    private NotifyIcon CreateTrayIcon(Icon appIcon)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Mostrar VENTORY", null, (_, _) => ShowFromTray());
        menu.Items.Add("Abrir ventory.bferal.cl", null, (_, _) => NavigateTo(PrimaryUrl));
        menu.Items.Add("Abrir ventory.tasker.cl", null, (_, _) => NavigateTo(FallbackUrl));
        menu.Items.Add(new ToolStripSeparator());

        var autostart = new ToolStripMenuItem("Iniciar con Windows") { Checked = IsAutostartEnabled() };
        ApplyFirstRunAutostart(autostart);
        autostart.Click += (_, _) =>
        {
            autostart.Checked = !autostart.Checked;
            SetAutostartEnabled(autostart.Checked);
        };
        menu.Items.Add(autostart);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Buscar actualizaciones…", null, (_, _) => _ = CheckForUpdatesInteractiveAsync());
        menu.Items.Add("Salir", null, (_, _) => ExitApplication());

        var trayIcon = (Icon)appIcon.Clone();
        var notify = new NotifyIcon
        {
            Icon = trayIcon,
            Text = BuildWindowTitle(),
            Visible = true,
            ContextMenuStrip = menu,
        };

        notify.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowFromTray();
        };

        return notify;
    }

    private void ShowFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        _webView.Focus();
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
    }

    private void NavigateTo(string url)
    {
        _activeUrl = url;
        SaveActiveUrl(url);
        if (_webView.CoreWebView2 is not null)
            _webView.CoreWebView2.Navigate(url);
        ShowFromTray();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.TaskManagerClosing)
        {
            e.Cancel = true;
            HideToTray();
        }
    }

    private void ExitApplication()
    {
        _updatePollTimer.Stop();
        _updatePollTimer.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        Application.Exit();
    }

    private static Icon LoadAppIcon()
    {
        foreach (var iconPath in ResolveIconPaths())
        {
            try
            {
                if (!File.Exists(iconPath)) continue;
                return new Icon(iconPath);
            }
            catch
            {
                /* siguiente ruta */
            }
        }

        try
        {
            var fromExe = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (fromExe is not null)
                return fromExe;
        }
        catch
        {
            /* ignore */
        }

        return SystemIcons.Application;
    }

    private static IEnumerable<string> ResolveIconPaths()
    {
        var baseDir = AppContext.BaseDirectory;
        yield return Path.Combine(baseDir, "icon-inv-favicon.ico");
        yield return Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "..", "ventory-desktop", "resources", "icons", "icon-inv-favicon.ico"));
        yield return Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "icon-inv-favicon.ico"));
    }

    private static string SettingsPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VentoryDesktop", "settings.txt");

    private static string SeenNotificationsPath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VentoryDesktop", "seen-notifications.txt");

    private string LoadActiveUrl()
    {
        try
        {
            var file = SettingsPath();
            if (!File.Exists(file)) return PrimaryUrl;
            var url = File.ReadAllText(file).Trim();
            return string.IsNullOrEmpty(url) ? PrimaryUrl : url;
        }
        catch
        {
            return PrimaryUrl;
        }
    }

    private void SaveActiveUrl(string url)
    {
        try
        {
            var file = SettingsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, url);
        }
        catch
        {
            /* ignore */
        }
    }

    private void LoadSeenNotificationIds()
    {
        try
        {
            var path = SeenNotificationsPath();
            if (!File.Exists(path)) return;
            foreach (var line in File.ReadAllLines(path))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    _seenNotificationIds.Add(line.Trim());
            }
        }
        catch
        {
            /* ignore */
        }
    }

    private void SaveSeenNotificationIds()
    {
        try
        {
            var path = SeenNotificationsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, _seenNotificationIds.Take(200));
        }
        catch
        {
            /* ignore */
        }
    }

    private static string DefaultsMarkerPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VentoryDesktop",
            "defaults.applied");

    private static void ApplyFirstRunAutostart(ToolStripMenuItem autostartItem)
    {
        if (File.Exists(DefaultsMarkerPath())) return;
        try
        {
            var dir = Path.GetDirectoryName(DefaultsMarkerPath())!;
            Directory.CreateDirectory(dir);
            SetAutostartEnabled(true);
            autostartItem.Checked = true;
            File.WriteAllText(DefaultsMarkerPath(), DateTimeOffset.UtcNow.ToString("o"));
        }
        catch
        {
            /* ignore */
        }
    }

    private static bool IsAutostartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue(RunKeyName) is string || key?.GetValue(LegacyRunKeyName) is string;
        }
        catch
        {
            return false;
        }
    }

    private static void SetAutostartEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
        if (key is null) return;

        key.DeleteValue(LegacyRunKeyName, throwOnMissingValue: false);

        if (enabled)
        {
            var exe = $"\"{Application.ExecutablePath}\" --tray-only";
            key.SetValue(RunKeyName, exe);
        }
        else
        {
            key.DeleteValue(RunKeyName, throwOnMissingValue: false);
        }
    }
}