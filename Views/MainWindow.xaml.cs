// File: Views/MainWindow.xaml.cs
using System.Windows;
using ImageDownloader.Services;
using ImageDownloader.ViewModels;

namespace ImageDownloader.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Создаём сервис и передаём его в MainViewModel
            var downloadService = new ImageDownloadService();
            DataContext = new MainViewModel(downloadService);
        }
    }
}
