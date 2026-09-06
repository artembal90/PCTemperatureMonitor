namespace PCTemperatureMonitor.Models;

public sealed record TemperatureReading(
    double? Cpu,
    double? Gpu,
    double? Motherboard,
    string? CpuName,
    string? GpuName,
    string? MotherboardName,
    DateTime Timestamp);
