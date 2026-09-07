using System.Text;

namespace PCTemperatureMonitor;

internal static class Program
{
    private static string LogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PCTemperatureMonitor",
        "startup.log");

    [STAThread]
    private static void Main()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] START\r\n", Encoding.UTF8);

            ApplicationConfiguration.Initialize();
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] AFTER INITIALIZE\r\n", Encoding.UTF8);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => HandleFatal(e.Exception, "UI thread");
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                HandleFatal(e.ExceptionObject as Exception ?? new Exception("Unknown AppDomain exception"), "AppDomain");
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                WriteLog(e.Exception, "Unobserved task");
                e.SetObserved();
            };

            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] BEFORE MAIN FORM\r\n", Encoding.UTF8);
            Application.Run(new MainForm());
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] NORMAL EXIT\r\n", Encoding.UTF8);
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
                $"PC Temperature Monitor не смог запуститься.\n\n{exception.GetType().Name}: {exception.Message}\n\nПодробности записаны в:\n{LogPath}",
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
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(
                LogPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {source}\r\n{exception}\r\n------------------------------\r\n",
                Encoding.UTF8);
        }
        catch
        {
        }
    }
}
