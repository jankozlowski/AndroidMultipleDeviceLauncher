using AndroidMultipleDeviceLauncher.Services;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Linq;
using System.IO;
using System.Windows.Controls;
using AndroidMultipleDeviceLauncher.Models;

namespace AndroidMultipleDeviceLauncher
{
    public partial class DevicePicker : Window
    {
        private readonly Adb Adb;
        private readonly Avd Avd;
        private readonly Settings Settings;

        public DevicePicker()
        {
            InitializeComponent();
            Adb = new Adb();
            Avd = new Avd();
            Settings = new Settings();

            LoadSettings();
            LoadData();
        }

        public void LoadData()
        {
            List<Device> connectedRealDevices = GetConnectedRealDevices();
            List<Device> avdEmulators = GetAvdEmulators();
            List<Device> devices = connectedRealDevices.Concat(avdEmulators).ToList();

            DeviceListView.ItemsSource = devices;
        }

        private void LoadSettings()
        {
            string adbPath = Settings.GetSetting(Settings.SettingsName, "adbPath");
            if (!string.IsNullOrEmpty(adbPath))
                AdbPathBox.Text = adbPath;

            string avdPath = Settings.GetSetting(Settings.SettingsName, "avdPath");
            if (!string.IsNullOrEmpty(avdPath))
                AvdPathBox.Text = avdPath;
        }

        private List<Device> GetAvdEmulators()
        {
            List<Device> devices = new List<Device>();

            string devicesAvdResult = Avd.AvdCommandWithResult("emulator -list-avds");
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

        private List<Device> GetConnectedRealDevices()
        {
            List<Device> devices = new List<Device>();

            string devicesAdbResult = Adb.AdbCommandWithResult("devices -l");

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
                    TypeImage = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/Phone.png"))
                };
                devices.Add(device);
            }

            foreach (Device device in devices)
            {
                string properties = Adb.AdbCommandWithResult($"-s {device.Id} shell getprop");
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
                device.Name = avdName;
            }

            return devices;
        }

        private string[] SplitByLine(string text)
        {
            string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            return lines;
        }

        private void CheckAdbClick(object sender, RoutedEventArgs e)
        {
            string adbPath = Path.Combine(AdbPathBox.Text, "adb.exe");
            bool adbExists = File.Exists(adbPath);
            if (adbExists)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Adb found at given path"), "Adb found");
            }
            else
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Adb not found at given path. Check if path is correct and contains adb.exe"), "Adb not found");
            }
        }

        private void CheckAvdClick(object sender, RoutedEventArgs e)
        {
            string avdPath = Path.Combine(AvdPathBox.Text, "emulator.exe");
            bool avdExists = File.Exists(avdPath);
            if (avdExists)
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Avd found at given path"), "Avd found");
            }
            else
            {
                MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Avd not found at given path. Check if path is correct and contains emulator.exe"), "Avd not found");
            }
        }

        private void AdbPathTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Adb == null)
                return;

            Adb.AdbPath = ((TextBox)e.Source).Text;
        }

        private void AvdPathTextChanged(object sender, TextChangedEventArgs e)
        {
            if (Avd == null)
                return;

            Avd.AvdPath = ((TextBox)e.Source).Text;
        }

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Settings.SaveSetting(Settings.SettingsName, "adbPath", AdbPathBox.Text);
            Settings.SaveSetting(Settings.SettingsName, "avdPath", AvdPathBox.Text);

            List<Device> selectedDevices = DeviceListView.Items.Cast<Device>().Where(d => d.IsChecked).ToList();
            SelectedDevicesSingelton.GetInstance().SelectedDevices = selectedDevices;
            Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
