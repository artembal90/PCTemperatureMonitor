namespace PCTemperatureMonitor.Models;

public sealed class AppSettings
{
    public double CpuWarning { get; set; } = 80;
    public double CpuCritical { get; set; } = 90;
    public double GpuWarning { get; set; } = 80;
    public double GpuCritical { get; set; } = 90;
    public double MotherboardWarning { get; set; } = 60;
    public double MotherboardCritical { get; set; } = 70;
    public int UpdateIntervalSeconds { get; set; } = 1;
    public bool StartWithWindows { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool EnableNotifications { get; set; } = true;
    public bool EnableSound { get; set; } = true;
}
