using AndroidMultipleDeviceLauncher.Models;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

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

        public List<Device> GetAvdEmulators()
        {
            List<Device> devices = new List<Device>();

            string devicesAvdResult = AvdCommandWithResult("emulator -list-avds");
            string[] lineResult = SplitByLine(devicesAvdResult);

            foreach (string line in lineResult)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                Device device = new Device
                {
                    Name = line,
                    TypeImage = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/Desktop.png")),
                    IsEmulator = true
                };
                devices.Add(device);
            }

            return devices;
        }

        private string[] SplitByLine(string text)
        {
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }

    }
}
