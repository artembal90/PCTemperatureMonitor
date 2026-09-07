using System.Text;

namespace PCTemperatureMonitor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => HandleFatal(e.Exception, "UI thread");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    WriteLog(ex, "AppDomain");
            };
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                WriteLog(e.Exception, "Unobserved task");
                e.SetObserved();
            };

            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            HandleFatal(ex, "Application startup");
        }
    }

    private static void HandleFatal(Exception exception, string source)
    {
        WriteLog(exception, source);

        try
        {
            MessageBox.Show(
                $"PC Temperature Monitor не смог запуститься.\n\n{exception.GetType().Name}: {exception.Message}\n\nПодробности записаны в:\n{GetLogPath()}",
                "PC Temperature Monitor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch
        {
        }
    }

    private static void WriteLog(Exception exception, string source)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PCTemperatureMonitor");
            Directory.CreateDirectory(directory);

            File.AppendAllText(
                Path.Combine(directory, "startup.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}\r\n{exception}\r\n------------------------------\r\n",
                Encoding.UTF8);
        }
        catch
        {
        }
    }

    private static string GetLogPath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PCTemperatureMonitor",
        "startup.log");
}
