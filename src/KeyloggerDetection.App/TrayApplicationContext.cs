using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.App;

/// <summary>
/// System tray application context managing the NotifyIcon lifecycle.
/// Proposal reference: § 2.1.5 — tray icon, right-click menu, monitoring control.
///
/// Right-click menu items (from proposal):
///   - Start Monitoring
///   - Stop Monitoring
///   - Open Logs
///   - Exit
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _startMenuItem;
    private readonly ToolStripMenuItem _stopMenuItem;

    private MonitoringState _state = MonitoringState.Stopped;
    private readonly IAppLogger _logger;
    private readonly IMonitoringCoordinator _coordinator;

    public TrayApplicationContext(IAppLogger logger, IMonitoringCoordinator coordinator)
    {
        _logger = logger;
        _coordinator = coordinator;

        if (_coordinator is App.MonitoringCoordinator appCoordinator)
        {
            appCoordinator.OnAlert += OnSuspiciousActivityDetected;
        }

        // Build the context menu exactly as specified in proposal § 2.1.5
        _startMenuItem = new ToolStripMenuItem("Start Monitoring", null, OnStartMonitoring);
        _stopMenuItem = new ToolStripMenuItem("Stop Monitoring", null, OnStopMonitoring);
        _stopMenuItem.Enabled = false; // Disabled when not monitoring

        var openLogsItem = new ToolStripMenuItem("Open Logs", null, OnOpenLogs);
        var exitItem = new ToolStripMenuItem("Exit", null, OnExit);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(_startMenuItem);
        contextMenu.Items.Add(_stopMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(openLogsItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        // Create the tray icon
        _notifyIcon = new NotifyIcon
        {
            Text = "KeylogSense — Monitoring Stopped",
            Icon = CreatePlaceholderIcon(Color.Gray),
            ContextMenuStrip = contextMenu,
            Visible = true
        };

        UpdateTrayState();
    }

    private void OnStartMonitoring(object? sender, EventArgs e)
    {
        _logger.LogInfo("User requested Start Monitoring.");
        _coordinator.Start();
        _state = MonitoringState.Running;
        UpdateTrayState();
    }

    private void OnStopMonitoring(object? sender, EventArgs e)
    {
        _logger.LogInfo("User requested Stop Monitoring.");
        _coordinator.Stop();
        _state = MonitoringState.Stopped;
        UpdateTrayState();
    }

    private void OnOpenLogs(object? sender, EventArgs e)
    {
        // Open the logs directory in File Explorer
        var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = logDir,
            UseShellExecute = true
        });
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _logger.LogInfo("User requested Exit. Shutting down application...");
        if (_state == MonitoringState.Running)
        {
            _coordinator.Stop();
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        Application.Exit();
    }

    /// <summary>
    /// Updates the tray icon and menu state based on the current monitoring state.
    /// </summary>
    private void UpdateTrayState()
    {
        if (_state == MonitoringState.Running)
        {
            _notifyIcon.Icon = CreatePlaceholderIcon(Color.Green);
            _notifyIcon.Text = "KeylogSense — Monitoring Active";
            _startMenuItem.Enabled = false;
            _stopMenuItem.Enabled = true;
        }
        else
        {
            _notifyIcon.Icon = CreatePlaceholderIcon(Color.Gray);
            _notifyIcon.Text = "KeylogSense — Monitoring Stopped";
            _startMenuItem.Enabled = true;
            _stopMenuItem.Enabled = false;
        }
    }

    /// <summary>
    /// Creates a simple coloured circle icon as a placeholder.
    /// Will be replaced with proper shield icons later.
    ///
    /// ENGINEERING ASSUMPTION: Placeholder icons are used until proper
    /// icon assets are created.
    /// </summary>
    private static Icon CreatePlaceholderIcon(Color color)
    {
        var bitmap = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 1, 1, 14, 14);

            // Draw a small "K" in the center for "KeylogSense"
            using var font = new Font("Segoe UI", 7f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.White);
            var textSize = g.MeasureString("K", font);
            g.DrawString("K", font, textBrush,
                (16 - textSize.Width) / 2,
                (16 - textSize.Height) / 2);
        }

        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    private void OnSuspiciousActivityDetected(DetectionResult result, string shortReasons)
    {
        // Must ensure UI thread executes notification
        if (_notifyIcon == null) return;
        
        var title = "KeylogSense Alert: Suspicious Activity";
        var text = $"Process: {result.ProcessIdentity.Name} (PID: {result.ProcessIdentity.Pid})\n" +
                   $"Risk Score: {result.TotalScore}/{result.Threshold}\n" +
                   $"Reasons: {shortReasons}";

        _notifyIcon.ShowBalloonTip(5000, title, text, ToolTipIcon.Warning);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _coordinator.Dispose();
        }
        base.Dispose(disposing);
    }
}
