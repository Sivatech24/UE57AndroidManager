using System;
using System.Threading;
using System.Windows;

namespace UE57AndroidManager
{
    public partial class ProgressWindow : Window
    {
        public CancellationTokenSource Cancellation { get; } = new CancellationTokenSource();
        public ProgressWindow()
        {
            InitializeComponent();
        }

        public void Report(double percent, long downloaded, long total)
        {
            Dispatcher.Invoke(() =>
            {
                if (percent >= 0)
                {
                    ProgressBar.Value = percent;
                    StatusText.Text = $"Downloading... {percent:F1}%";
                }
                else
                {
                    StatusText.Text = "Downloading...";
                }
                BytesText.Text = total > 0 ? $"{downloaded} / {total} bytes" : $"{downloaded} bytes";
            });
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelButton.IsEnabled = false;
            Cancellation.Cancel();
            StatusText.Text = "Cancelling...";
        }
    }
}
