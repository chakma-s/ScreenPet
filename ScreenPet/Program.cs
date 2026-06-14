using System;
using System.Threading;
using System.Windows.Forms;

namespace ScreenPet;

static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        // Single-instance guard — only one ScreenPet at a time
        _mutex = new Mutex(true, "ScreenPet_6F4A2B1C", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "ScreenPet is already running!\nCheck your system tray.",
                "ScreenPet",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var petWindow = new PetWindow();
            petWindow.Show();
            Application.Run();          // Windows message pump — single-threaded
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }
}
