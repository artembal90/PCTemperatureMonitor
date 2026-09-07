using System.Runtime.InteropServices;
using System.Text;

namespace PCTemperatureMonitor;

internal static class Program
{
    private static readonly string[] LogPaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCTemperatureMonitor", "startup.log"),
        Path.Combine(Path.GetTempPath(), "PCTemperatureMonitor-startup.log"),
        Path.Combine(AppContext.BaseDirectory, "startup.log")
    ];

    [STAThread]
    private static void Main()
    {
        StartupLog.Mark("START");
        StartupLog.Mark($"OS={Environment.OSVersion}; Process={RuntimeInformation.ProcessArchitecture}; Runtime={Environment.Version}; Base={AppContext.BaseDirectory}");

        try
        {
            StartupLog.Mark("BEFORE INITIALIZE");
            ApplicationConfiguration.Initialize();
            StartupLog.Mark("AFTER INITIALIZE");

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => HandleFatal(e.Exception, "UI thread");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                HandleFatal(e.ExceptionObject as Exception ?? new Exception("Unknown AppDomain exception"), "AppDomain");
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                StartupLog.Error(e.Exception, "Unobserved task");
                e.SetObserved();
            };

            StartupLog.Mark("BEFORE MAIN FORM");
            using var form = new MainForm();
            StartupLog.Mark("AFTER MAIN FORM CONSTRUCTOR");
            Application.Run(form);
            StartupLog.Mark("NORMAL EXIT");
        }
        catch (Exception ex)
        {
            HandleFatal(ex, "Application startup");
        }
    }

    private static void HandleFatal(Exception exception, string source)
    {
        StartupLog.Error(exception, source);
        try
        {
            MessageBox.Show(
                $"PC Temperature Monitor не смог запуститься.\n\n{exception.GetType().Name}: {exception.Message}\n\nЛоги:\n{string.Join("\n", LogPaths)}",
                "PC Temperature Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch { }
    }

    internal static class StartupLog
    {
        internal static void Mark(string message) => Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");

        internal static void Error(Exception exception, string source) =>
            Write($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}\r\n{exception}\r\n------------------------------");

        private static void Write(string message)
        {
            foreach (var path in LogPaths.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                    File.AppendAllText(path, message + Environment.NewLine, Encoding.UTF8);
                }
                catch { }
            }
        }
    }
}
