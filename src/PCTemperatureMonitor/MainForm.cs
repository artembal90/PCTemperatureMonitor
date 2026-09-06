using System.Media;
using Microsoft.Win32;
using PCTemperatureMonitor.Models;
using PCTemperatureMonitor.Services;

namespace PCTemperatureMonitor;

public sealed class MainForm : Form
{
    private readonly SettingsService _settingsService = new();
    private readonly NotifyIcon _trayIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private AppSettings _settings;
    private HardwareMonitorService? _hardware;
    private Label _cpuValue = null!;
    private Label _gpuValue = null!;
    private Label _mbValue = null!;
    private Label _statusValue = null!;
    private Label _updatedValue = null!;
    private Panel _cpuPanel = null!;
    private Panel _gpuPanel = null!;
    private Panel _mbPanel = null!;
    private bool _allowClose;
    private AlertState _cpuAlert;
    private AlertState _gpuAlert;
    private AlertState _mbAlert;

    public MainForm()
    {
        _settings = _settingsService.Load();
        Text = "PC Temperature Monitor";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(440, 390);
        MinimumSize = new Size(440, 390);
        MaximumSize = new Size(440, 390);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Color.FromArgb(245, 247, 250);
        BuildUi();

        _trayIcon = new NotifyIcon { Text = "PC Temperature Monitor", Visible = true, Icon = SystemIcons.Application, ContextMenuStrip = BuildTrayMenu() };
        _trayIcon.DoubleClick += (_, _) => ShowFromTray();
        _timer = new System.Windows.Forms.Timer { Interval = Math.Clamp(_settings.UpdateIntervalSeconds, 1, 10) * 1000 };
        _timer.Tick += async (_, _) => await UpdateTemperaturesAsync();
        _timer.Start();
        UpdateStartupSetting();
        Shown += async (_, _) => await UpdateTemperaturesAsync();
        FormClosing += MainForm_FormClosing;

        try
        {
            _hardware = new HardwareMonitorService();
        }
        catch (Exception ex)
        {
            _statusValue.Text = "Статус: мониторинг датчиков недоступен";
            _statusValue.ForeColor = Color.Firebrick;
            _updatedValue.Text = $"Ошибка инициализации: {ex.Message}";
        }
    }

    private void BuildUi()
    {
        Controls.Add(new Label { Text = "Температура ПК", Font = new Font("Segoe UI", 18, FontStyle.Bold), AutoSize = true, Location = new Point(24, 18) });
        var settingsButton = new Button { Text = "⚙", Font = new Font("Segoe UI Symbol", 14), Size = new Size(44, 36), Location = new Point(370, 12), FlatStyle = FlatStyle.Flat };
        settingsButton.FlatAppearance.BorderSize = 0;
        settingsButton.Click += (_, _) => ShowSettings();
        Controls.Add(settingsButton);
        _cpuPanel = CreateSensorPanel("Процессор", 65, out _cpuValue);
        _gpuPanel = CreateSensorPanel("Видеокарта", 145, out _gpuValue);
        _mbPanel = CreateSensorPanel("Материнская плата", 225, out _mbValue);
        Controls.AddRange([_cpuPanel, _gpuPanel, _mbPanel]);
        _statusValue = new Label { Text = "Статус: определение…", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(24, 307) };
        _updatedValue = new Label { Text = "Обновлено: —", AutoSize = true, ForeColor = Color.DimGray, Location = new Point(24, 334) };
        Controls.Add(_statusValue);
        Controls.Add(_updatedValue);
        var hideButton = new Button { Text = "Скрыть в трей", Size = new Size(130, 32), Location = new Point(280, 325) };
        hideButton.Click += (_, _) => HideToTray();
        Controls.Add(hideButton);
    }

    private Panel CreateSensorPanel(string name, int y, out Label valueLabel)
    {
        var panel = new Panel { Location = new Point(20, y), Size = new Size(400, 68), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
        panel.Controls.Add(new Label { Text = name, AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), Location = new Point(14, 10) });
        valueLabel = new Label { Text = "— °C", AutoSize = true, Font = new Font("Segoe UI", 21, FontStyle.Bold), Location = new Point(270, 7) };
        panel.Controls.Add(valueLabel);
        return panel;
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Открыть", null, (_, _) => ShowFromTray());
        menu.Items.Add("Настройки", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Выход", null, (_, _) => { _allowClose = true; Close(); });
        return menu;
    }

    private async Task UpdateTemperaturesAsync()
    {
        if (_hardware is null) return;
        TemperatureReading reading;
        try { reading = await Task.Run(_hardware.Read); }
        catch (Exception ex)
        {
            _statusValue.Text = "Статус: ошибка чтения датчиков";
            _statusValue.ForeColor = Color.Firebrick;
            _updatedValue.Text = $"{ex.GetType().Name}: {ex.Message}";
            return;
        }
        SetValue(_cpuValue, _cpuPanel, reading.Cpu, "CPU", ref _cpuAlert, _settings.CpuWarning, _settings.CpuCritical);
        SetValue(_gpuValue, _gpuPanel, reading.Gpu, "GPU", ref _gpuAlert, _settings.GpuWarning, _settings.GpuCritical);
        SetValue(_mbValue, _mbPanel, reading.Motherboard, "Материнская плата", ref _mbAlert, _settings.MotherboardWarning, _settings.MotherboardCritical);
        var allMissing = reading.Cpu is null && reading.Gpu is null && reading.Motherboard is null;
        var critical = _cpuAlert == AlertState.Critical || _gpuAlert == AlertState.Critical || _mbAlert == AlertState.Critical;
        var warning = !critical && (_cpuAlert == AlertState.Warning || _gpuAlert == AlertState.Warning || _mbAlert == AlertState.Warning);
        _statusValue.Text = allMissing ? "Статус: датчики не найдены" : critical ? "Статус: ⚠ КРИТИЧЕСКАЯ ТЕМПЕРАТУРА" : warning ? "Статус: Повышенная температура" : "Статус: Норма";
        _statusValue.ForeColor = allMissing ? Color.DimGray : critical ? Color.Firebrick : warning ? Color.DarkOrange : Color.SeaGreen;
        _updatedValue.Text = $"Обновлено: {reading.Timestamp:HH:mm:ss}";
    }

    private void SetValue(Label label, Panel panel, double? value, string name, ref AlertState state, double warning, double critical)
    {
        var nextState = value is null ? AlertState.Unknown : value.Value >= critical ? AlertState.Critical : value.Value >= warning ? AlertState.Warning : AlertState.Normal;
        label.Text = value is null ? "— °C" : $"{value.Value:0} °C";
        label.ForeColor = nextState switch { AlertState.Critical => Color.Firebrick, AlertState.Warning => Color.DarkOrange, _ => Color.FromArgb(35, 40, 45) };
        panel.BackColor = nextState switch { AlertState.Critical => Color.FromArgb(255, 235, 235), AlertState.Warning => Color.FromArgb(255, 247, 225), _ => Color.White };
        if (nextState == AlertState.Critical && state != AlertState.Critical) Alert(name, value!.Value, true);
        else if (nextState == AlertState.Warning && state == AlertState.Normal) Alert(name, value!.Value, false);
        state = nextState;
    }

    private void Alert(string name, double value, bool critical)
    {
        if (_settings.EnableSound) SystemSounds.Exclamation.Play();
        if (_settings.EnableNotifications)
        {
            _trayIcon.BalloonTipTitle = critical ? "⚠ Критическая температура" : "Повышенная температура";
            _trayIcon.BalloonTipText = $"{name}: {value:0} °C";
            _trayIcon.ShowBalloonTip(5000);
        }
    }

    private void ShowSettings()
    {
        using var dialog = new SettingsForm(_settings);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _settings = dialog.Result;
        _settingsService.Save(_settings);
        _timer.Interval = Math.Clamp(_settings.UpdateIntervalSeconds, 1, 10) * 1000;
        UpdateStartupSetting();
        _cpuAlert = _gpuAlert = _mbAlert = AlertState.Unknown;
    }

    private void UpdateStartupSetting()
    {
        const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string valueName = "PCTemperatureMonitor";
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKey, writable: true) ?? Registry.CurrentUser.CreateSubKey(runKey);
            if (key is null) return;
            if (_settings.StartWithWindows) key.SetValue(valueName, $"\"{Application.ExecutablePath}\"");
            else key.DeleteValue(valueName, throwOnMissingValue: false);
        }
        catch { }
    }

    private void HideToTray() => Hide();
    private void ShowFromTray() { Show(); WindowState = FormWindowState.Normal; Activate(); }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_allowClose && _settings.MinimizeToTray) { e.Cancel = true; HideToTray(); return; }
        _timer.Stop();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _hardware?.Dispose();
    }

    private enum AlertState { Unknown, Normal, Warning, Critical }
}
