namespace FlytLauncher;

internal static class Program
{
    private const string MutexName = "FlytRP_Launcher_SingleInstance";

    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            MessageBox.Show(
                "Лаунчер уже запущен.",
                "FlytRP Launcher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
