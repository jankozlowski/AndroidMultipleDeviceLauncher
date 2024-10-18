using AndroidMultipleDeviceLauncher.Services;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace AndroidMultipleDeviceLauncher
{
    public partial class DevicePicker : Window
    {
        private readonly Adb Adb;
        private readonly Avd Avd;

        public DevicePicker()
        {
            InitializeComponent();
            Adb = new Adb();
            Avd = new Avd();

            List<Device> connectedRealDevices = GetConnectedRealDevices();
            List<Device> avdEmulators = GetAvdEmulators();
            List<Device> devices = connectedRealDevices.Concat(avdEmulators).ToList();

            DeviceListView.ItemsSource = devices;
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
                    TypeImage = new BitmapImage(new Uri($"pack://application:,,,/{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name};component/Resources/Desktop.png"))
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

        private class Device
        {
            public bool IsChecked { get; set; }

            public ImageSource TypeImage { get; set; }

            public string Name { get; set; }

            public string Id { get; set; }
        }
    }
}
