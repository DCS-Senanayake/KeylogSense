using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using KeyloggerDetection.Core.Configuration;
using KeyloggerDetection.Core.Interfaces;
using KeyloggerDetection.Core.Models;

namespace KeyloggerDetection.App;

internal sealed class DashboardForm : Form
{
    private DetectionConfig _config;
    private readonly bool _isElevated;
    private readonly Func<MonitoringState> _getState;
    private readonly Action _onStart;
    private readonly Action _onStop;
    private readonly Action _onOpenLogs;
    private readonly Func<IReadOnlyList<TrayApplicationContext.DashboardAlert>> _getAlerts;
    private readonly IAppLogger _logger;
    private readonly Action<DetectionConfig> _saveConfig;

    private TabControl _tabControl = null!;

    // Live Monitoring Tab
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

    // Previous Logs Tab
    private ComboBox _cboLogFiles = null!;
    private DataGridView _gridLogs = null!;
    private Button _btnRefreshLogsList = null!;
    private Button _btnDeleteLog = null!;
    private Button _btnOpenLogsFolder = null!;

    // Allowlist Tab
    private ListBox _lstTrustedPublishers = null!;
    private ListBox _lstTrustedPaths = null!;
    private ListBox _lstTrustedNames = null!;
    private TextBox _txtAddAllowlist = null!;
    private ComboBox _cboAllowlistType = null!;
    private Button _btnAddAllowlist = null!;
    private Button _btnRemoveAllowlist = null!;
    private Button _btnSaveAllowlist = null!;

    // Configuration Tab
    private NumericUpDown _numAlertThreshold = null!;
    private NumericUpDown _numMonitoringInterval = null!;
    private NumericUpDown _numSmallWriteCount = null!;
    private NumericUpDown _numSmallWriteMaxBytes = null!;
    private NumericUpDown _numRepeatedWriteThreshold = null!;
    private NumericUpDown _numRepeatedWriteWindow = null!;
    private NumericUpDown _numNetworkPollingInterval = null!;
    private NumericUpDown _numOutboundConnectionCount = null!;
    private NumericUpDown _numFileNetWindow = null!;
    private NumericUpDown _numPersistencePolling = null!;
    private Button _btnSaveConfig = null!;
    private Button _btnResetConfig = null!;

    public DashboardForm(
        DetectionConfig config,
        bool isElevated,
        Func<MonitoringState> getState,
        Action onStart,
        Action onStop,
        Action onOpenLogs,
        Func<IReadOnlyList<TrayApplicationContext.DashboardAlert>> getAlerts,
        IAppLogger logger,
        Action<DetectionConfig> saveConfig)
    {
        _config = config;
        _isElevated = isElevated;
        _getState = getState;
        _onStart = onStart;
        _onStop = onStop;
        _onOpenLogs = onOpenLogs;
        _getAlerts = getAlerts;
        _logger = logger;
        _saveConfig = saveConfig;

        InitializeComponent();
        RefreshData();
    }

    private void InitializeComponent()
    {
        this.Text = "KeylogSense";
        this.Size = new Size(1000, 700);
        this.MinimumSize = new Size(800, 600);
        this.StartPosition = FormStartPosition.CenterScreen;

        _tabControl = new TabControl { Dock = DockStyle.Fill, Padding = new Point(10, 5) };
        
        var tabLive = new TabPage("Live Monitoring") { Padding = new Padding(10) };
        var tabLogs = new TabPage("Previous Logs") { Padding = new Padding(10) };
        var tabAllowlist = new TabPage("Allowlist") { Padding = new Padding(10) };
        var tabConfig = new TabPage("Configuration") { Padding = new Padding(10) };

        _tabControl.TabPages.Add(tabLive);
        _tabControl.TabPages.Add(tabLogs);
        _tabControl.TabPages.Add(tabAllowlist);
        _tabControl.TabPages.Add(tabConfig);

        this.Controls.Add(_tabControl);

        InitLiveMonitoringTab(tabLive);
        InitPreviousLogsTab(tabLogs);
        InitAllowlistTab(tabAllowlist);
        InitConfigurationTab(tabConfig);
        
        _tabControl.SelectedIndexChanged += (s, e) => {
            if (_tabControl.SelectedTab == tabLogs) RefreshLogsList();
            if (_tabControl.SelectedTab == tabAllowlist) LoadAllowlistUI();
            if (_tabControl.SelectedTab == tabConfig) LoadConfigUI();
        };
    }

    // --- LIVE MONITORING TAB ---

    private void InitLiveMonitoringTab(TabPage tab)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 130));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        tab.Controls.Add(layout);

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
        
        // Context menu for Allowlisting
        var ctxMenu = new ContextMenuStrip();
        var itemAddAllowlist = new ToolStripMenuItem("Add Process to Allowlist");
        itemAddAllowlist.Click += (s, e) => {
            if (_gridAlerts.SelectedRows.Count > 0) {
                var row = _gridAlerts.SelectedRows[0];
                var alert = row.DataBoundItem as TrayApplicationContext.DashboardAlert;
                if (alert != null) {
                    string pName = alert.ProcessName;
                    if (!pName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) pName += ".exe";
                    if (!_config.Allowlist.TrustedProcessNames.Contains(pName, StringComparer.OrdinalIgnoreCase)) {
                        var list = _config.Allowlist.TrustedProcessNames.ToList();
                        list.Add(pName);
                        _config.Allowlist.TrustedProcessNames = list.ToArray();
                        _saveConfig(_config);
                        _logger.LogInfo($"Dashboard: Added {pName} to trusted process names allowlist.");
                        MessageBox.Show($"Added {pName} to allowlist.", "Allowlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        };
        ctxMenu.Items.Add(itemAddAllowlist);
        _gridAlerts.ContextMenuStrip = ctxMenu;

        layout.Controls.Add(_gridAlerts, 0, 1);

        var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _btnStart = new Button { Text = "Start Monitoring", Width = 130, Height = 30 };
        _btnStart.Click += (s, e) => { _onStart(); RefreshData(); };
        
        _btnStop = new Button { Text = "Stop Monitoring", Width = 130, Height = 30 };
        _btnStop.Click += (s, e) => { _onStop(); RefreshData(); };

        _btnLogs = new Button { Text = "Open Logs Folder", Width = 130, Height = 30 };
        _btnLogs.Click += (s, e) => _onOpenLogs();

        _btnRefresh = new Button { Text = "Refresh", Width = 110, Height = 30 };
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

    // --- PREVIOUS LOGS TAB ---

    private void InitPreviousLogsTab(TabPage tab)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        tab.Controls.Add(layout);

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _cboLogFiles = new ComboBox { Width = 250, DropDownStyle = ComboBoxStyle.DropDownList, Margin = new Padding(0, 4, 10, 0) };
        _cboLogFiles.SelectedIndexChanged += (s, e) => LoadSelectedLogFile();

        _btnRefreshLogsList = new Button { Text = "Refresh List", Width = 120, Height = 30 };
        _btnRefreshLogsList.Click += (s, e) => RefreshLogsList();

        _btnDeleteLog = new Button { Text = "Delete File", Width = 120, Height = 30 };
        _btnDeleteLog.Click += (s, e) => DeleteSelectedLog();

        _btnOpenLogsFolder = new Button { Text = "Open Logs Folder", Width = 130, Height = 30 };
        _btnOpenLogsFolder.Click += (s, e) => _onOpenLogs();

        topPanel.Controls.AddRange(new Control[] { new Label { Text = "Select Log File:", AutoSize = true, Padding = new Padding(0, 8, 5, 0) }, _cboLogFiles, _btnRefreshLogsList, _btnDeleteLog, _btnOpenLogsFolder });
        layout.Controls.Add(topPanel, 0, 0);

        _gridLogs = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            RowHeadersVisible = false
        };
        layout.Controls.Add(_gridLogs, 0, 1);
    }

    private string GetLogsDirectory()
    {
        return string.IsNullOrWhiteSpace(_config.LogDirectory) 
            ? Path.Combine(AppContext.BaseDirectory, "Logs") 
            : Path.Combine(AppContext.BaseDirectory, _config.LogDirectory);
    }

    private void RefreshLogsList()
    {
        _logger.LogInfo("Dashboard: Opening previous log viewer / refreshing log list.");
        _cboLogFiles.Items.Clear();
        var dir = GetLogsDirectory();
        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, "*.csv").Select(Path.GetFileName).OrderByDescending(f => f).ToArray();
            _cboLogFiles.Items.AddRange(files);
            if (files.Length > 0) _cboLogFiles.SelectedIndex = 0;
        }
    }

    private void LoadSelectedLogFile()
    {
        if (_cboLogFiles.SelectedItem == null) {
            _gridLogs.DataSource = null;
            return;
        }

        string fileName = _cboLogFiles.SelectedItem.ToString()!;
        string filePath = Path.Combine(GetLogsDirectory(), fileName);

        if (!File.Exists(filePath)) return;

        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0) return;

            var dt = new DataTable();
            var headers = ParseCsvLine(lines[0]);
            foreach (var h in headers) dt.Columns.Add(h);

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var rowData = ParseCsvLine(lines[i]);
                while (rowData.Count < dt.Columns.Count) rowData.Add("");
                dt.Rows.Add(rowData.Take(dt.Columns.Count).ToArray());
            }

            _gridLogs.DataSource = dt;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Dashboard: Failed to load log file {fileName}", ex);
            MessageBox.Show($"Failed to load log file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var pattern = @"(((?<x>(?=[,\r\n]+))|""(?<x>([^""]|"""")+)""|(?<x>[^,\r\n]+)),?)";
        foreach (Match m in Regex.Matches(line, pattern))
        {
            if (m.Success)
            {
                result.Add(m.Groups["x"].Value.Replace("\"\"", "\""));
            }
        }
        return result;
    }

    private void DeleteSelectedLog()
    {
        if (_cboLogFiles.SelectedItem == null) return;
        string fileName = _cboLogFiles.SelectedItem.ToString()!;
        string filePath = Path.Combine(GetLogsDirectory(), fileName);

        var confirm = MessageBox.Show($"Are you sure you want to delete {fileName}?\nThis action cannot be undone.", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm == DialogResult.Yes)
        {
            try
            {
                File.Delete(filePath);
                _logger.LogInfo($"Dashboard: Deleted log file {fileName}");
                RefreshLogsList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Dashboard: Failed to delete log file {fileName}", ex);
                MessageBox.Show($"Failed to delete file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // --- ALLOWLIST TAB ---

    private void InitAllowlistTab(TabPage tab)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 3, Padding = new Padding(10) };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        tab.Controls.Add(layout);

        _lstTrustedPublishers = new ListBox { Dock = DockStyle.Fill };
        _lstTrustedPaths = new ListBox { Dock = DockStyle.Fill };
        _lstTrustedNames = new ListBox { Dock = DockStyle.Fill };

        var pnlPub = new GroupBox { Text = "Trusted Publishers", Dock = DockStyle.Fill };
        pnlPub.Controls.Add(_lstTrustedPublishers);
        layout.Controls.Add(pnlPub, 0, 0);

        var pnlPath = new GroupBox { Text = "Trusted Paths", Dock = DockStyle.Fill };
        pnlPath.Controls.Add(_lstTrustedPaths);
        layout.Controls.Add(pnlPath, 1, 0);

        var pnlName = new GroupBox { Text = "Trusted Process Names", Dock = DockStyle.Fill };
        pnlName.Controls.Add(_lstTrustedNames);
        layout.Controls.Add(pnlName, 2, 0);

        var inputPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        _cboAllowlistType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        _cboAllowlistType.Items.AddRange(new[] { "Publisher", "Path", "Process Name" });
        _cboAllowlistType.SelectedIndex = 0;
        
        _txtAddAllowlist = new TextBox { Width = 300 };
        _btnAddAllowlist = new Button { Text = "Add", Width = 80 };
        _btnAddAllowlist.Click += (s, e) => AddAllowlistEntry();

        _btnRemoveAllowlist = new Button { Text = "Remove Selected", Width = 120 };
        _btnRemoveAllowlist.Click += (s, e) => RemoveAllowlistEntry();

        inputPanel.Controls.AddRange(new Control[] { _cboAllowlistType, _txtAddAllowlist, _btnAddAllowlist, _btnRemoveAllowlist });
        layout.Controls.Add(inputPanel, 0, 1);
        layout.SetColumnSpan(inputPanel, 3);

        var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        _btnSaveAllowlist = new Button { Text = "Save Allowlist", Width = 120, Height = 30 };
        _btnSaveAllowlist.Click += (s, e) => SaveAllowlist();
        bottomPanel.Controls.Add(_btnSaveAllowlist);
        layout.Controls.Add(bottomPanel, 0, 2);
        layout.SetColumnSpan(bottomPanel, 3);
    }

    private void LoadAllowlistUI()
    {
        _lstTrustedPublishers.Items.Clear();
        _lstTrustedPublishers.Items.AddRange(_config.Allowlist.TrustedPublishers.Cast<object>().ToArray());

        _lstTrustedPaths.Items.Clear();
        _lstTrustedPaths.Items.AddRange(_config.Allowlist.TrustedExecutablePaths.Cast<object>().ToArray());

        _lstTrustedNames.Items.Clear();
        _lstTrustedNames.Items.AddRange(_config.Allowlist.TrustedProcessNames.Cast<object>().ToArray());
    }

    private void AddAllowlistEntry()
    {
        var type = _cboAllowlistType.SelectedItem?.ToString();
        var val = _txtAddAllowlist.Text.Trim();
        if (string.IsNullOrEmpty(val)) return;

        if (type == "Publisher" && !_lstTrustedPublishers.Items.Contains(val)) _lstTrustedPublishers.Items.Add(val);
        else if (type == "Path" && !_lstTrustedPaths.Items.Contains(val)) _lstTrustedPaths.Items.Add(val);
        else if (type == "Process Name" && !_lstTrustedNames.Items.Contains(val)) _lstTrustedNames.Items.Add(val);

        _txtAddAllowlist.Text = "";
    }

    private void RemoveAllowlistEntry()
    {
        if (_lstTrustedPublishers.SelectedIndex >= 0)
            _lstTrustedPublishers.Items.RemoveAt(_lstTrustedPublishers.SelectedIndex);
        
        if (_lstTrustedPaths.SelectedIndex >= 0)
            _lstTrustedPaths.Items.RemoveAt(_lstTrustedPaths.SelectedIndex);
        
        if (_lstTrustedNames.SelectedIndex >= 0)
            _lstTrustedNames.Items.RemoveAt(_lstTrustedNames.SelectedIndex);
    }

    private void SaveAllowlist()
    {
        _config.Allowlist.TrustedPublishers = _lstTrustedPublishers.Items.Cast<string>().ToArray();
        _config.Allowlist.TrustedExecutablePaths = _lstTrustedPaths.Items.Cast<string>().ToArray();
        _config.Allowlist.TrustedProcessNames = _lstTrustedNames.Items.Cast<string>().ToArray();

        _saveConfig(_config);
        _logger.LogInfo("Dashboard: Saved new allowlist configuration.");
        MessageBox.Show("Allowlist saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // --- CONFIGURATION TAB ---

    private void InitConfigurationTab(TabPage tab)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 12, ColumnCount = 2, Padding = new Padding(20) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tab.Controls.Add(layout);

        int row = 0;

        _numAlertThreshold = AddConfigRow(layout, row++, "Alert Threshold:", 1, 100);
        _numMonitoringInterval = AddConfigRow(layout, row++, "Monitoring Interval (ms):", 100, 60000);
        _numSmallWriteCount = AddConfigRow(layout, row++, "Small Write Count Threshold:", 1, 1000);
        _numSmallWriteMaxBytes = AddConfigRow(layout, row++, "Small Write Max Bytes:", 1, 100000);
        _numRepeatedWriteThreshold = AddConfigRow(layout, row++, "Repeated Same File Write Threshold:", 1, 1000);
        _numRepeatedWriteWindow = AddConfigRow(layout, row++, "Repeated Write Window (s):", 1, 3600);
        _numNetworkPollingInterval = AddConfigRow(layout, row++, "Network Polling Interval (ms):", 100, 60000);
        _numOutboundConnectionCount = AddConfigRow(layout, row++, "Outbound Connection Count Threshold:", 1, 1000);
        _numFileNetWindow = AddConfigRow(layout, row++, "File/Network Correlation Window (s):", 1, 3600);
        _numPersistencePolling = AddConfigRow(layout, row++, "Persistence Polling Interval (ms):", 1000, 360000);

        var bottomPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(0, 20, 0, 0) };
        _btnSaveConfig = new Button { Text = "Save Configuration", Width = 150, Height = 30 };
        _btnSaveConfig.Click += (s, e) => SaveConfigFromUI();
        
        _btnResetConfig = new Button { Text = "Reset to Defaults", Width = 150, Height = 30 };
        _btnResetConfig.Click += (s, e) => ResetConfigUI();

        var lblNote = new Label { Text = "Note: Changes to intervals may require stopping and starting monitoring.", AutoSize = true, Padding = new Padding(10, 5, 0, 0), ForeColor = Color.DimGray };

        bottomPanel.Controls.AddRange(new Control[] { _btnSaveConfig, _btnResetConfig, lblNote });
        layout.Controls.Add(bottomPanel, 0, row);
        layout.SetColumnSpan(bottomPanel, 2);
    }

    private NumericUpDown AddConfigRow(TableLayoutPanel layout, int row, string labelText, int min, int max)
    {
        var lbl = new Label { Text = labelText, AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var num = new NumericUpDown { Minimum = min, Maximum = max, Width = 120 };
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(num, 1, row);
        return num;
    }

    private void LoadConfigUI()
    {
        _numAlertThreshold.Value = _config.AlertThreshold;
        _numMonitoringInterval.Value = _config.MonitoringIntervalMs;
        _numSmallWriteCount.Value = _config.SmallWriteCountThreshold;
        _numSmallWriteMaxBytes.Value = _config.SmallWriteMaxBytes;
        _numRepeatedWriteThreshold.Value = _config.RepeatedSameFileWriteThreshold;
        _numRepeatedWriteWindow.Value = _config.RepeatedWriteWindowSeconds;
        _numNetworkPollingInterval.Value = _config.NetworkPollingIntervalMs;
        _numOutboundConnectionCount.Value = _config.OutboundConnectionCountThreshold;
        _numFileNetWindow.Value = _config.FileNetworkCorrelationWindowSeconds;
        _numPersistencePolling.Value = _config.PersistencePollingIntervalMs;
    }

    private void SaveConfigFromUI()
    {
        _config.AlertThreshold = (int)_numAlertThreshold.Value;
        _config.MonitoringIntervalMs = (int)_numMonitoringInterval.Value;
        _config.SmallWriteCountThreshold = (int)_numSmallWriteCount.Value;
        _config.SmallWriteMaxBytes = (int)_numSmallWriteMaxBytes.Value;
        _config.RepeatedSameFileWriteThreshold = (int)_numRepeatedWriteThreshold.Value;
        _config.RepeatedWriteWindowSeconds = (int)_numRepeatedWriteWindow.Value;
        _config.NetworkPollingIntervalMs = (int)_numNetworkPollingInterval.Value;
        _config.OutboundConnectionCountThreshold = (int)_numOutboundConnectionCount.Value;
        _config.FileNetworkCorrelationWindowSeconds = (int)_numFileNetWindow.Value;
        _config.PersistencePollingIntervalMs = (int)_numPersistencePolling.Value;

        _saveConfig(_config);
        _logger.LogInfo("Dashboard: Saved new thresholds/configuration.");
        MessageBox.Show("Configuration saved successfully.\nRestart monitoring if intervals were changed.", "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        RefreshData();
    }

    private void ResetConfigUI()
    {
        var confirm = MessageBox.Show("Are you sure you want to reset all configurations to their default values? Allowlist will be preserved.", "Reset to Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (confirm == DialogResult.Yes)
        {
            var def = new DetectionConfig();
            _config.AlertThreshold = def.AlertThreshold;
            _config.MonitoringIntervalMs = def.MonitoringIntervalMs;
            _config.SmallWriteCountThreshold = def.SmallWriteCountThreshold;
            _config.SmallWriteMaxBytes = def.SmallWriteMaxBytes;
            _config.RepeatedSameFileWriteThreshold = def.RepeatedSameFileWriteThreshold;
            _config.RepeatedWriteWindowSeconds = def.RepeatedWriteWindowSeconds;
            _config.NetworkPollingIntervalMs = def.NetworkPollingIntervalMs;
            _config.OutboundConnectionCountThreshold = def.OutboundConnectionCountThreshold;
            _config.FileNetworkCorrelationWindowSeconds = def.FileNetworkCorrelationWindowSeconds;
            _config.PersistencePollingIntervalMs = def.PersistencePollingIntervalMs;

            _saveConfig(_config);
            LoadConfigUI();
            _logger.LogInfo("Dashboard: Reset thresholds/configuration to defaults.");
            MessageBox.Show("Configuration reset to defaults.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshData();
        }
    }
}
