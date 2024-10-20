using AndroidMultipleDeviceLauncher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

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

        public List<Device> GetConnectedRealDevices()
        {
            List<Device> devices = new List<Device>();

            string devicesAdbResult = AdbCommandWithResult("devices -l");

            string[] lineResult = SplitByLine(devicesAdbResult);

            foreach (string line in lineResult.Skip(1))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.ToLower().Contains("emulator"))
                    continue;

                string[] parameters = line.Split();
                Device device = new Device
                {
                    Id = parameters[0],
                    IsEmulator = false,
                    TypeImage = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/Phone.png"))
                };
                devices.Add(device);
            }

            foreach (Device device in devices)
            {
                string avdName = GetDeviceName(device);
                device.Name = avdName;
            }

            return devices;
        }

        public List<Device> GetConnectedDevices()
        {
            List<Device> devices = new List<Device>();

            string devicesAdbResult = AdbCommandWithResult("devices -l");

            string[] lineResult = SplitByLine(devicesAdbResult);

            foreach (string line in lineResult.Skip(1))
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                string[] parameters = line.Split();
                Device device = new Device
                {
                    Id = parameters[0],
                    IsEmulator = line.ToLower().Contains("emulator"),
                    TypeImage = line.ToLower().Contains("emulator") ? new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/Desktop.png")) : new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/Phone.png"))
                };
                devices.Add(device);
            }

            foreach (Device device in devices)
            {
                string avdName = GetDeviceName(device);
                device.Name = avdName;
            }

            return devices;
        }

        private string GetDeviceName(Device device)
        {
            string properties = AdbCommandWithResult($"-s {device.Id} shell getprop");
            string[] propertieslines = SplitByLine(properties);
            string avdNameLine = propertieslines.Where(l => l.Contains("ro.boot.qemu.avd_name")).FirstOrDefault();

            if (string.IsNullOrEmpty(avdNameLine))
                avdNameLine = propertieslines.Where(l => l.Contains("ro.product.device")).FirstOrDefault();
            if (string.IsNullOrEmpty(avdNameLine))
                avdNameLine = propertieslines.Where(l => l.Contains("ro.product.product.model")).FirstOrDefault();
            if (string.IsNullOrEmpty(avdNameLine))
                avdNameLine = propertieslines.Where(l => l.Contains("ro.boot.hardware.sku")).FirstOrDefault();
            if (string.IsNullOrEmpty(avdNameLine))
                avdNameLine = ":Unknown_Device";

            string avdName = avdNameLine.Split(':')[1];
            avdName = avdName.Trim().Replace("_", " ");
            avdName = avdName.Substring(1, avdName.Length - 2);
            avdName = avdName[0].ToString().ToUpper() + avdName.Substring(1);

            return avdName;
        }


        private string[] SplitByLine(string text)
        {
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }
    }
}
