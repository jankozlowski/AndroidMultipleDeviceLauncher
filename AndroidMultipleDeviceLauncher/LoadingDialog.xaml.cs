using System.Threading.Tasks;
using System.Windows;
using System;
using System.Threading;

namespace AndroidMultipleDeviceLauncher
{
    public partial class LoadingDialog : Window, IDisposable
    {
        private CancellationTokenSource cancellationTokenSource;

        public LoadingDialog(CancellationTokenSource cts)
        {
            InitializeComponent();
            cancellationTokenSource = cts;
        }

        public void SetMessage(string newMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                loadingLabel.Text = newMessage;
            });
        }

        public void Dispose()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Close();
            });
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
            Dispose();
        }
    }
}