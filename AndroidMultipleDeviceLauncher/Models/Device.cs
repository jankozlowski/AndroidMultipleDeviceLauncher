using System.Windows.Media;

namespace AndroidMultipleDeviceLauncher.Models
{
    public class Device
    {
        public string Id { get; set; }

        public bool IsChecked { get; set; }

        public bool IsEmulator { get; set; }

        public ImageSource TypeImage { get; set; }

        public string AvdName { get; set; }

        public string AdbName { get; set; }

    }
}
