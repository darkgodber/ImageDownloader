using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ImageDownloader.Services;

namespace ImageDownloader.ViewModels
{
    /// <summary>
    /// ViewModel «главного окна»: держит коллекцию ImageItemViewModel,
    /// общий прогресс и команду «Start All».
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        #region Fields

        private readonly IImageDownloadService _downloadService;
        private double _overallProgress;

        #endregion

        #region Properties

        /// <summary>Три «слота» для загрузки картинок.</summary>
        public ObservableCollection<ImageItemViewModel> Items { get; }

        /// <summary>Общий прогресс (максимум среди всех Progress в Items).</summary>
        public double OverallProgress
        {
            get => _overallProgress;
            private set => SetProperty(ref _overallProgress, value);
        }

        #endregion

        #region Commands

        /// <summary>Запускает загрузку сразу по всем заполненным URL.</summary>
        public ICommand StartAllCommand { get; }

        #endregion

        #region Constructor

        public MainViewModel(IImageDownloadService downloadService)
        {
            _downloadService = downloadService
                ?? throw new ArgumentNullException(nameof(downloadService));

            // 1) Инициализируем коллекцию из трёх слотов
            Items = CreateSlots();

            // 2) Подписываемся на изменения Progress/Url
            SubscribeToItems();

            // 3) Формируем команду «Start All»
            StartAllCommand = new RelayCommand(
                ExecuteStartAll,
                CanStartAll
            );

            // 4) Задаём начальный общий прогресс
            UpdateOverallProgress();
        }

        #endregion

        #region Initialization

        private ObservableCollection<ImageItemViewModel> CreateSlots()
            => new ObservableCollection<ImageItemViewModel>(
                   Enumerable.Range(0, 3)
                             .Select(_ => new ImageItemViewModel(_downloadService))
               );

        private void SubscribeToItems()
        {
            foreach (var item in Items)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        #endregion

        #region Event Handlers

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ImageItemViewModel.Progress):
                    UpdateOverallProgress();
                    break;

                case nameof(ImageItemViewModel.Url):
                    // Чтобы CanExecute у StartAllCommand обновился
                    CommandManager.InvalidateRequerySuggested();
                    break;
            }
        }

        #endregion

        #region Command Callbacks

        private bool CanStartAll(object _)
            // разрешаем, если хоть один URL выглядит «валидно» с точки зрения расширения
            => Items.Any(i => IsValidUrl(i.Url));

        private void ExecuteStartAll(object _)
        {
            foreach (var item in Items)
            {
                if (!string.IsNullOrWhiteSpace(item.Url))
                    item.StartCommand.Execute(null);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Пересчитывает OverallProgress как максимум из всех Progress.
        /// </summary>
        private void UpdateOverallProgress()
        {
            OverallProgress = Items.Any()
                ? Items.Max(i => i.Progress)
                : 0;
        }

        /// <summary>
        /// Простая синтаксическая проверка URL + расширение,
        /// чтобы кнопка StartAll не активировалась на всякий «foo.jpg».
        /// </summary>
        private static bool IsValidUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u) ||
                (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
                return false;

            var ext = Path.GetExtension(u.LocalPath)
                          .ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".gif";
        }

        #endregion
    }
}
