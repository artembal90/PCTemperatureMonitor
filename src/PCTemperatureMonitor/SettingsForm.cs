using PCTemperatureMonitor.Models;

namespace PCTemperatureMonitor;

public sealed class SettingsForm : Form
{
    private readonly NumericUpDown _cpuWarn = N(80);
    private readonly NumericUpDown _cpuCrit = N(90);
    private readonly NumericUpDown _gpuWarn = N(80);
    private readonly NumericUpDown _gpuCrit = N(90);
    private readonly NumericUpDown _mbWarn = N(60);
    private readonly NumericUpDown _mbCrit = N(70);
    private readonly NumericUpDown _interval = N(1, 1, 10);
    private readonly CheckBox _startup = new() { Text = "Запускать вместе с Windows", AutoSize = true };
    private readonly CheckBox _tray = new() { Text = "Сворачивать в трей при закрытии", AutoSize = true, Checked = true };
    private readonly CheckBox _notifications = new() { Text = "Показывать уведомления", AutoSize = true, Checked = true };
    private readonly CheckBox _sound = new() { Text = "Звуковой сигнал", AutoSize = true, Checked = true };

    public AppSettings Result { get; private set; }

    public SettingsForm(AppSettings current)
    {
        Result = current;
        Text = "Настройки температуры";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 455);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;

        AddRow("CPU предупреждение", _cpuWarn, 20);
        AddRow("CPU критическая", _cpuCrit, 58);
        AddRow("GPU предупреждение", _gpuWarn, 96);
        AddRow("GPU критическая", _gpuCrit, 134);
        AddRow("Мат. плата предупреждение", _mbWarn, 172);
        AddRow("Мат. плата критическая", _mbCrit, 210);
        AddRow("Интервал обновления (сек)", _interval, 248);

        _startup.Location = new Point(20, 291);
        _tray.Location = new Point(20, 319);
        _notifications.Location = new Point(20, 347);
        _sound.Location = new Point(20, 375);
        Controls.AddRange([_startup, _tray, _notifications, _sound]);

        var cancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Size = new Size(100, 32), Location = new Point(198, 407) };
        var save = new Button { Text = "Сохранить", DialogResult = DialogResult.OK, Size = new Size(100, 32), Location = new Point(306, 407) };
        save.Click += (_, _) => SaveResult();
        Controls.AddRange([cancel, save]);
        AcceptButton = save;
        CancelButton = cancel;

        LoadValues(current);
    }

    private void LoadValues(AppSettings s)
    {
        _cpuWarn.Value = ToDecimal(s.CpuWarning); _cpuCrit.Value = ToDecimal(s.CpuCritical);
        _gpuWarn.Value = ToDecimal(s.GpuWarning); _gpuCrit.Value = ToDecimal(s.GpuCritical);
        _mbWarn.Value = ToDecimal(s.MotherboardWarning); _mbCrit.Value = ToDecimal(s.MotherboardCritical);
        _interval.Value = Math.Clamp(s.UpdateIntervalSeconds, 1, 10);
        _startup.Checked = s.StartWithWindows;
        _tray.Checked = s.MinimizeToTray;
        _notifications.Checked = s.EnableNotifications;
        _sound.Checked = s.EnableSound;
    }

    private void SaveResult()
    {
        Result = new AppSettings
        {
            CpuWarning = (double)_cpuWarn.Value,
            CpuCritical = (double)_cpuCrit.Value,
            GpuWarning = (double)_gpuWarn.Value,
            GpuCritical = (double)_gpuCrit.Value,
            MotherboardWarning = (double)_mbWarn.Value,
            MotherboardCritical = (double)_mbCrit.Value,
            UpdateIntervalSeconds = (int)_interval.Value,
            StartWithWindows = _startup.Checked,
            MinimizeToTray = _tray.Checked,
            EnableNotifications = _notifications.Checked,
            EnableSound = _sound.Checked
        };
    }

    private void AddRow(string labelText, NumericUpDown input, int y)
    {
        var label = new Label { Text = labelText, AutoSize = true, Location = new Point(20, y + 6) };
        input.Location = new Point(300, y);
        input.Size = new Size(80, 24);
        Controls.Add(label);
        Controls.Add(input);
    }

    private static NumericUpDown N(decimal value, decimal min = 0, decimal max = 120) => new() { Minimum = min, Maximum = max, Value = value };
    private static decimal ToDecimal(double value) => Math.Clamp((decimal)value, 0, 120);
}
