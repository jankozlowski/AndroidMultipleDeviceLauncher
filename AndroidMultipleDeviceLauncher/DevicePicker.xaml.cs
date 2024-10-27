using AndroidMultipleDeviceLauncher.Services;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.IO;
using System.Windows.Controls;
using AndroidMultipleDeviceLauncher.Models;
using Microsoft.VisualStudio.Shell;
using System.Windows.Media;

namespace AndroidMultipleDeviceLauncher
{
    public partial class DevicePicker : Window
    {
        private readonly Adb Adb;
        private readonly Avd Avd;
        private readonly Settings Settings;
        private List<Device> CurrentlySelectedDevices;

        public DevicePicker()
        {
            InitializeComponent();
            Adb = new Adb();
            Avd = new Avd();
            Settings = new Settings();

            SolidColorBrush systemColor = (SolidColorBrush)FindResource(VsBrushes.WindowTextKey);
            VSSettings.DarkMode = systemColor.Color.ToString().Equals("#FFFAFAFA");

            LoadSettings();
            LoadData();
        }

        public void LoadData()
        {
            List<Device> connectedRealDevices = Adb.GetConnectedRealDevices();
            List<Device> avdEmulators = Avd.GetAvdEmulators();
            List<Device> devices = connectedRealDevices.Concat(avdEmulators).ToList();

            devices = CheckDevice(devices, SelectedDevicesSingelton.GetInstance().SelectedDevices);

            DeviceListView.ItemsSource = devices;
        }

        public void RefreshData()
        {
            List<Device> connectedRealDevices = Adb.GetConnectedRealDevices();
            List<Device> avdEmulators = Avd.GetAvdEmulators();
            List<Device> devices = connectedRealDevices.Concat(avdEmulators).ToList();

            devices = CheckDevice(devices, CurrentlySelectedDevices);

            DeviceListView.ItemsSource = devices;
        }

        private List<Device> CheckDevice(List<Device> foundDevices, List<Device> rememberedDevices)
        {
            foreach (Device device in rememberedDevices)
            {
                if (device.IsEmulator)
                {
                    var selectedDevice = foundDevices.Where(d => d.IsEmulator && d.AvdName.Equals(device.AvdName)).FirstOrDefault();
                    if (selectedDevice != null)
                    {
                        selectedDevice.IsChecked = true;
                    }
                }
                if (!device.IsEmulator)
                {
                    var selectedDevice = foundDevices.Where(d => !d.IsEmulator && d.Id.Equals(device.Id)).FirstOrDefault();
                    if (selectedDevice != null)
                    {
                        selectedDevice.IsChecked = true;
                    }
                }
            }

            return foundDevices;
        }

        private void LoadSettings()
        {
            string adbPath = Settings.GetSetting(Settings.SettingsName, "adbPath");
            if (!string.IsNullOrEmpty(adbPath))
                AdbPathBox.Text = adbPath;

            string avdPath = Settings.GetSetting(Settings.SettingsName, "avdPath");
            if (!string.IsNullOrEmpty(avdPath))
                AvdPathBox.Text = avdPath;

            string buildCheckBoxString = Settings.GetSetting(Settings.SettingsName, "buildSolution");
            bool buildCheckBoxBool = false;
            bool.TryParse(buildCheckBoxString, out buildCheckBoxBool);
            BuildCheckBox.IsChecked = buildCheckBoxBool;
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
            CurrentlySelectedDevices = DeviceListView.ItemsSource.Cast<Device>().Where(d => d.IsChecked).ToList();
            RefreshData();
        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            Settings.SaveSetting(Settings.SettingsName, "adbPath", AdbPathBox.Text);
            Settings.SaveSetting(Settings.SettingsName, "avdPath", AvdPathBox.Text);
            Settings.SaveSetting(Settings.SettingsName, "buildSolution", BuildCheckBox.IsChecked.ToString());

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
