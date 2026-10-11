using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace PrivacyShield;

public partial class MainWindow : Window
{
    private const string BlockRuleName = "PrivacyShield-BlockLAN";

    private const string PrivateRanges =
        "10.0.0.0/8,172.16.0.0/12,192.168.0.0/16,169.254.0.0/16";

    private static readonly string[] FingerprintFlags =
    {
        "--disable-reading-from-canvas",
        "--blink-settings=audioContextEnabled=false",
        "--disable-gpu-rasterization",
        "--disable-plugins",
        "--disable-shared-workers"
    };

    private const string DisableWebRtcFlag = "--disable-webrtc";

    private static string FiveMFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FiveM");

    private bool _suppressEvents;

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings();
        UpdateFirewallStatus();
    }

    private void Drag_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            try { DragMove(); } catch { }
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void LoadSettings()
    {
        _suppressEvents = true;

        var settings = AppSettings.Load();

        if (string.IsNullOrWhiteSpace(settings.FiveMPath))
            settings.FiveMPath = Path.Combine(FiveMFolder, "FiveM.exe");

        txtPath.Text = settings.FiveMPath;
        txtArgs.Text = settings.Arguments;
        tglClearCache.IsChecked = settings.ClearCache;
        tglFingerprint.IsChecked = settings.BlockFingerprinting;
        tglWebRtc.IsChecked = settings.DisableWebRtc;

        _suppressEvents = false;
    }

    private void SaveSettings()
    {
        var settings = AppSettings.Load();
        settings.FiveMPath = txtPath.Text.Trim();
        settings.Arguments = txtArgs.Text.Trim();
        settings.ClearCache = tglClearCache.IsChecked == true;
        settings.BlockFingerprinting = tglFingerprint.IsChecked == true;
        settings.DisableWebRtc = tglWebRtc.IsChecked == true;
        settings.Save();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "FiveM executable (FiveM.exe)|FiveM.exe|Executable (*.exe)|*.exe",
            Title = "Locate FiveM.exe"
        };

        if (Directory.Exists(FiveMFolder))
            dialog.InitialDirectory = FiveMFolder;

        if (dialog.ShowDialog() == true)
            txtPath.Text = dialog.FileName;
    }

    private string BuildArguments()
    {
        var parts = new List<string>();

        string baseArgs = txtArgs.Text.Trim();
        if (baseArgs.Length > 0)
            parts.Add(baseArgs);

        if (tglFingerprint.IsChecked == true)
            parts.AddRange(FingerprintFlags);

        if (tglWebRtc.IsChecked == true)
            parts.Add(DisableWebRtcFlag);

        return string.Join(" ", parts);
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        SaveSettings();

        if (tglClearCache.IsChecked == true)
            ClearNuiCache();

        string exe = txtPath.Text.Trim();
        if (!File.Exists(exe))
        {
            SetStatus("Couldn't find FiveM.exe at that path.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = BuildArguments(),
                UseShellExecute = true
            });
            SetStatus("Launched FiveM.");
        }
        catch (Exception ex)
        {
            SetStatus("Launch failed: " + ex.Message);
        }
    }

    private void ClearNuiCache()
    {
        string cache = Path.Combine(FiveMFolder, "FiveM.app", "cache", "nui-storage");
        try
        {
            if (Directory.Exists(cache))
                Directory.Delete(cache, recursive: true);
        }
        catch (Exception ex)
        {
            SetStatus("Couldn't clear the cache (is FiveM still open?): " + ex.Message);
        }
    }

    private void Firewall_Toggled(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents)
            return;

        if (tglFirewall.IsChecked == true)
        {
            if (!AddBlockRules())
            {
                _suppressEvents = true;
                tglFirewall.IsChecked = BlockRulesPresent();
                _suppressEvents = false;
            }
        }
        else
        {
            RemoveBlockRules();
        }

        UpdateFirewallStatus();
    }

    private List<string> FindFiveMExecutables()
    {
        string appFolder = Path.Combine(FiveMFolder, "FiveM.app");
        if (!Directory.Exists(appFolder))
            return new List<string>();

        try
        {
            return new List<string>(Directory.GetFiles(appFolder, "*.exe", SearchOption.AllDirectories));
        }
        catch
        {
            return new List<string>();
        }
    }

    private bool AddBlockRules()
    {
        var exes = FindFiveMExecutables();
        if (exes.Count == 0)
        {
            SetStatus("No FiveM executables found. Run the game once, then try again.");
            return false;
        }

        var script = new StringBuilder();
        script.AppendLine("@echo off");
        script.AppendLine($"netsh advfirewall firewall delete rule name=\"{BlockRuleName}\" >nul 2>&1");

        foreach (string exe in exes)
        {
            script.AppendLine(
                $"netsh advfirewall firewall add rule name=\"{BlockRuleName}\" " +
                $"dir=out action=block program=\"{exe}\" remoteip={PrivateRanges} enable=yes");
        }

        if (RunAsAdmin(script.ToString()))
        {
            SetStatus($"LAN block applied to {exes.Count} FiveM executable(s).");
            return true;
        }

        SetStatus("Firewall change was cancelled.");
        return false;
    }

    private void RemoveBlockRules()
    {
        string script = "@echo off\r\n" +
                        $"netsh advfirewall firewall delete rule name=\"{BlockRuleName}\"\r\n";

        SetStatus(RunAsAdmin(script) ? "LAN block removed." : "Firewall change was cancelled.");
    }

    private static bool RunAsAdmin(string script)
    {
        string scriptPath = Path.Combine(Path.GetTempPath(), $"psshield_{Guid.NewGuid():N}.bat");
        try
        {
            File.WriteAllText(scriptPath, script);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"\"{scriptPath}\"\"",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            });

            process?.WaitForExit();
            return process is { ExitCode: 0 };
        }
        catch
        {
            return false;
        }
        finally
        {
            try { if (File.Exists(scriptPath)) File.Delete(scriptPath); } catch { }
        }
    }

    private static bool BlockRulesPresent()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo(
                "netsh", $"advfirewall firewall show rule name=\"{BlockRuleName}\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null)
                return false;

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Contains(BlockRuleName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void UpdateFirewallStatus()
    {
        bool active = BlockRulesPresent();

        _suppressEvents = true;
        tglFirewall.IsChecked = active;
        _suppressEvents = false;

        txtNetStatus.Text = active
            ? "Active. Re-apply after each FiveM update by toggling it off then on."
            : "Requires admin. Re-apply after each FiveM update by toggling it off then on.";
    }

    private void SetStatus(string message) => txtStatus.Text = message;
}
