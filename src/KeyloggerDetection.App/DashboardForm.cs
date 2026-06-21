using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.App;

internal sealed class DashboardForm : Form
{
    private readonly DetectionConfig _config;
    private readonly bool _isElevated;
    private readonly Func<MonitoringState> _getState;
    private readonly Action _onStart;
    private readonly Action _onStop;
    private readonly Action _onOpenLogs;
    private readonly Func<IReadOnlyList<TrayApplicationContext.DashboardAlert>> _getAlerts;

    private Label _lblStatus = null!;
    private Label _lblTelemetry = null!;
    private Label _lblAlertCount = null!;
    private Label _lblLastAlert = null!;
    private Label _lblThreshold = null!;
    private DataGridView _gridAlerts = null!;
    private Button _btnStart = null!;
    private Button _btnStop = null!;
    private Button _btnLogs = null!;
    private Button _btnRefresh = null!;

    public DashboardForm(
        DetectionConfig config,
        bool isElevated,
        Func<MonitoringState> getState,
        Action onStart,
        Action onStop,
        Action onOpenLogs,
        Func<IReadOnlyList<TrayApplicationContext.DashboardAlert>> getAlerts)
    {
        _config = config;
        _isElevated = isElevated;
        _getState = getState;
        _onStart = onStart;
        _onStop = onStop;
        _onOpenLogs = onOpenLogs;
        _getAlerts = getAlerts;

        InitializeComponent();
        RefreshData();
    }

    private void InitializeComponent()
    {
        this.Text = "KeylogSense Dashboard";
        this.Size = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterScreen;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(10)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        this.Controls.Add(layout);

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        _lblStatus = new Label { AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        _lblTelemetry = new Label { AutoSize = true };
        _lblAlertCount = new Label { AutoSize = true };
        _lblLastAlert = new Label { AutoSize = true };
        _lblThreshold = new Label { AutoSize = true };
        
        topPanel.Controls.AddRange(new Control[] { _lblStatus, _lblTelemetry, _lblAlertCount, _lblLastAlert, _lblThreshold });
        layout.Controls.Add(topPanel, 0, 0);

        _gridAlerts = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false
        };
        
        _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Time", HeaderText = "Time", FillWeight = 15 });
        _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ProcessName", HeaderText = "Process Name", FillWeight = 20 });
        _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Pid", HeaderText = "PID", FillWeight = 10 });
        _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Score", HeaderText = "Score", FillWeight = 10 });
        _gridAlerts.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Reasons", HeaderText = "Reasons", FillWeight = 45 });
        
        layout.Controls.Add(_gridAlerts, 0, 1);

        var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _btnStart = new Button { Text = "Start Monitoring", Width = 120, Height = 30 };
        _btnStart.Click += (s, e) => { _onStart(); RefreshData(); };
        
        _btnStop = new Button { Text = "Stop Monitoring", Width = 120, Height = 30 };
        _btnStop.Click += (s, e) => { _onStop(); RefreshData(); };

        _btnLogs = new Button { Text = "Open Logs", Width = 100, Height = 30 };
        _btnLogs.Click += (s, e) => _onOpenLogs();

        _btnRefresh = new Button { Text = "Refresh", Width = 100, Height = 30 };
        _btnRefresh.Click += (s, e) => RefreshData();

        bottomPanel.Controls.AddRange(new Control[] { _btnStart, _btnStop, _btnLogs, _btnRefresh });
        layout.Controls.Add(bottomPanel, 0, 2);
    }

    public void RefreshData()
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(RefreshData));
            return;
        }

        var state = _getState();
        _lblStatus.Text = $"Monitoring Status: {state}";
        _btnStart.Enabled = state == MonitoringState.Stopped;
        _btnStop.Enabled = state == MonitoringState.Running;

        string fileTelemetry = _isElevated ? "Active (ETW)" : "Offline (Requires Admin)";
        _lblTelemetry.Text = $"Telemetry: Process (Active), File ({fileTelemetry}), Network (Active), Persistence (Active)";

        var alerts = _getAlerts();
        _lblAlertCount.Text = $"Total Alerts: {alerts.Count}";
        _lblLastAlert.Text = alerts.Count > 0 ? $"Last Alert: {alerts[alerts.Count - 1].Time:T}" : "Last Alert: None";

        _lblThreshold.Text = $"Threshold: {_config.AlertThreshold} | Loc: {_config.SuspiciousLocationScore}, Pub: {_config.UntrustedPublisherScore}, SmallWrites: {_config.FrequentSmallWritesScore}, RepWrites: {_config.RepeatedSameFileWritesScore}, Net: {_config.OutboundNetworkScore}, Corr: {_config.FileNetworkCorrelationScore}, Persist: {_config.PersistenceDetectedScore}";

        _gridAlerts.DataSource = null;
        _gridAlerts.DataSource = new List<TrayApplicationContext.DashboardAlert>(alerts);
    }
}
