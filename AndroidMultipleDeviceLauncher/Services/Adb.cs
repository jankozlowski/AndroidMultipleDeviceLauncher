using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AndroidMultipleDeviceLauncher.Services
{
    public class Adb
    {
        public static string AdbPath = string.Empty;

        public void AdbCommand(string command)
        {
            if (!CheckAdbPath())
                return;

            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = AdbExePath();
                cmd.StartInfo.Arguments = command;
                cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = false;

                cmd.Start();
                cmd.WaitForExit();
            }
        }

        public string AdbCommandWithResult(string command)
        {
            if (!CheckAdbPath())
                return string.Empty;

            string output = string.Empty;

            using (Process cmd = new Process())
            {
                cmd.StartInfo.FileName = AdbExePath();
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

        private string AdbExePath()
        {
            return Path.Combine(AdbPath, "adb.exe");
        }

        private bool CheckAdbPath()
        {
            bool adbExists = File.Exists(AdbExePath());
            if (adbExists)
            {
                return true;
            }

            MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Adb not found at given path. Check if path is correct and contains adb.exe"), "Adb not found");
            return false;
        }
    }
}
