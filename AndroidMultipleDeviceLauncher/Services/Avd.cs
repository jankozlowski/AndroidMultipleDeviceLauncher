using System.Diagnostics;
using System.IO;
using System.Windows;

namespace AndroidMultipleDeviceLauncher.Services
{
    public class Avd
    {
        public static string AvdPath = string.Empty;

        public void AvdCommand(string command)
        {
            if (!CheckAvdPath())
                return;

            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = AvdExePath();
                cmd.StartInfo.Arguments = command;
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;

                cmd.Start();
                cmd.WaitForExit();
            }
        }

        public string AvdCommandWithResult(string command)
        {
            if (!CheckAvdPath())
                return string.Empty;

            string output = string.Empty;

            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = AvdExePath();
                cmd.StartInfo.Arguments = command;
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;

                cmd.Start();
                output = cmd.StandardOutput.ReadToEnd();
                cmd.WaitForExit();
            }

            return output;
        }

        private string AvdExePath()
        {
            return Path.Combine(AvdPath, "emulator.exe");
        }

        private bool CheckAvdPath()
        {
            bool avdExists = File.Exists(AvdExePath());
            if (avdExists)
            {
                return true;
            }

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Avd not found at given path. Check if path is correct and contains emulator.exe"), "Adb not found");
            return false;
        }

    }
}
