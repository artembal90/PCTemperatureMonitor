using LibreHardwareMonitor.Hardware;
using PCTemperatureMonitor.Models;

namespace PCTemperatureMonitor.Services;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private readonly UpdateVisitor _visitor = new();
    private bool _disposed;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true,
            IsStorageEnabled = false,
            IsMemoryEnabled = false,
            IsNetworkEnabled = false
        };

        _computer.Open();
    }

    public TemperatureReading Read()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(HardwareMonitorService));

        _visitor.Clear();
        _computer.Accept(_visitor);

        var cpu = FindBestTemperature(_visitor.CpuSensors, out var cpuName);
        var gpu = FindBestTemperature(_visitor.GpuSensors, out var gpuName);
        var motherboard = FindBestTemperature(_visitor.MotherboardSensors, out var motherboardName);

        return new TemperatureReading(cpu, gpu, motherboard, cpuName, gpuName, motherboardName, DateTime.Now);
    }

    private static double? FindBestTemperature(IEnumerable<TemperatureSensor> sensors, out string? sensorName)
    {
        sensorName = null;
        var valid = sensors.Where(x => x.Value is double && x.Value > -20 && x.Value < 150).ToList();
        if (valid.Count == 0) return null;

        TemperatureSensor? selected = valid.FirstOrDefault(s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
            ?? valid.FirstOrDefault(s => s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase))
            ?? valid.FirstOrDefault(s => s.Name.Contains("GPU Core", StringComparison.OrdinalIgnoreCase))
            ?? valid.FirstOrDefault(s => s.Name.Equals("Core", StringComparison.OrdinalIgnoreCase))
            ?? valid.FirstOrDefault();

        sensorName = selected?.Name;
        return selected?.Value;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _computer.Close(); } catch { }
    }

    private sealed class UpdateVisitor : IVisitor
    {
        public List<TemperatureSensor> CpuSensors { get; } = [];
        public List<TemperatureSensor> GpuSensors { get; } = [];
        public List<TemperatureSensor> MotherboardSensors { get; } = [];

        public void Clear()
        {
            CpuSensors.Clear();
            GpuSensors.Clear();
            MotherboardSensors.Clear();
        }

        public void VisitComputer(IComputer computer) { computer.Traverse(this); }
        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            hardware.Accept(this);
        }
        public void VisitSensor(ISensor sensor)
        {
            if (sensor.SensorType != SensorType.Temperature || sensor.Value is null) return;
            var item = new TemperatureSensor(sensor.Name, sensor.Value.Value);
            switch (sensor.Hardware.HardwareType)
            {
                case HardwareType.Cpu: CpuSensors.Add(item); break;
                case HardwareType.GpuAmd:
                case HardwareType.GpuNvidia:
                case HardwareType.GpuIntel: GpuSensors.Add(item); break;
                case HardwareType.Motherboard: MotherboardSensors.Add(item); break;
            }
        }
        public void VisitParameter(IParameter parameter) { }
    }

    private sealed record TemperatureSensor(string Name, double Value);
}
