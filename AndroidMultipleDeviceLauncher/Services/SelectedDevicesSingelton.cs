using AndroidMultipleDeviceLauncher.Models;
using System.Collections.Generic;

namespace AndroidMultipleDeviceLauncher.Services
{
    public sealed class SelectedDevicesSingelton
    {
        private SelectedDevicesSingelton() { }

        private static SelectedDevicesSingelton _instance;

        public List<Device> SelectedDevices { get; set; }

        public static SelectedDevicesSingelton GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SelectedDevicesSingelton();
            }
            return _instance;
        }
    }
}
