using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using ImageDownloader.Services;

namespace ImageDownloader.ViewModels
{
    public class ImageItemViewModel : BaseViewModel, IDataErrorInfo
    {
        #region Constants & Static

        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>Допустимые расширения изображений.</summary>
        private static readonly string[] SupportedExtensions =
            { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        #endregion

        #region Fields

        private readonly IImageDownloadService _downloadService;
        private CancellationTokenSource _cts;

        private string _url;
        private bool _urlFormatIsValid;
        private bool _urlResourceIsValid;
        private bool _isCheckingUrl;
        private string _errorMessage;
        private BitmapImage _image;
        private double _progress;
        private bool _isDownloading;

        #endregion

        #region Public Properties

        /// <summary>URL для загрузки.</summary>
        public string Url
        {
            get => _url;
            set
            {
                if (!SetProperty(ref _url, value)) return;
                ResetValidationState();
                _ = ValidateUrlAsync(_url);
            }
        }

        /// <summary>Флаг, что URL корректен по формату и расширению.</summary>
        public bool UrlFormatIsValid
        {
            get => _urlFormatIsValid;
            private set => SetProperty(ref _urlFormatIsValid, value);
        }

        /// <summary>Флаг, что ресурс доступен и является изображением.</summary>
        public bool UrlResourceIsValid
        {
            get => _urlResourceIsValid;
            private set => SetProperty(ref _urlResourceIsValid, value);
        }

        /// <summary>Идёт ли сейчас проверка URL.</summary>
        public bool IsCheckingUrl
        {
            get => _isCheckingUrl;
            private set
            {
                if (SetProperty(ref _isCheckingUrl, value))
                    OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>Текст последней ошибки валидации или скачивания.</summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (SetProperty(ref _errorMessage, value))
                    OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>Скачанное изображение.</summary>
        public BitmapImage Image
        {
            get => _image;
            private set => SetProperty(ref _image, value);
        }

        /// <summary>Текущий прогресс загрузки (0–100).</summary>
        public double Progress
        {
            get => _progress;
            private set => SetProperty(ref _progress, value);
        }

        /// <summary>Идёт ли сейчас загрузка.</summary>
        public bool IsDownloading
        {
            get => _isDownloading;
            private set
            {
                if (SetProperty(ref _isDownloading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        /// <summary>Читабельный статус (в процессе, ошибка, готово, ожидает).</summary>
        public string Status
        {
            get
            {
                if (IsCheckingUrl) return "Проверяю URL…";
                if (!string.IsNullOrEmpty(ErrorMessage))
                    return "Ошибка: " + ErrorMessage;
                if (IsDownloading) return "Скачиваю…";
                if (Image != null) return "Готово";
                return "Ожидает";
            }
        }

        #endregion

        #region Commands

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }

        #endregion

        #region Constructor

        public ImageItemViewModel(IImageDownloadService downloadService)
        {
            _downloadService = downloadService
                ?? throw new ArgumentNullException(nameof(downloadService));

            StartCommand = new RelayCommand(
                async _ => await StartDownloadAsync(),
                _ => UrlFormatIsValid && UrlResourceIsValid && !IsDownloading
            );

            StopCommand = new RelayCommand(
                _ => _cts?.Cancel(),
                _ => IsDownloading
            );
        }

        #endregion

        #region URL Validation

        private void ResetValidationState()
        {
            UrlFormatIsValid = false;
            UrlResourceIsValid = false;
            ErrorMessage = null;
            IsCheckingUrl = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task ValidateUrlAsync(string url)
        {
            try
            {
                // 1) Формат и расширение
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    ErrorMessage = "Неверный формат URL";
                    return;
                }

                var ext = Path.GetExtension(uri.LocalPath)
                              .ToLowerInvariant();
                if (Array.IndexOf(SupportedExtensions, ext) < 0)
                {
                    ErrorMessage = $"Расширение {ext} не поддерживается";
                    return;
                }

                UrlFormatIsValid = true;

                // 2) HEAD-запрос: 404, статус и Content-Type
                using var req = new HttpRequestMessage(HttpMethod.Head, uri);
                using var resp = await HttpClient.SendAsync(req);

                if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ErrorMessage = "Ошибка 404: ресурс не найден";
                    return;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    ErrorMessage = $"Сервер вернул {(int)resp.StatusCode} {resp.ReasonPhrase}";
                    return;
                }

                var ct = resp.Content.Headers.ContentType?.MediaType;
                if (ct == null || !ct.StartsWith("image/"))
                {
                    ErrorMessage = "Ответ не содержит изображение";
                    return;
                }

                UrlResourceIsValid = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsCheckingUrl = false;
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(Url));
            }
        }

        #endregion

        #region Download Logic

        private async Task StartDownloadAsync()
        {
            _cts = new CancellationTokenSource();
            IsDownloading = true;
            Progress = 0;

            try
            {
                using var stream = await _downloadService
                    .DownloadImageAsync(Url, new Progress<double>(p => Progress = p), _cts.Token);

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = stream;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();

                Image = bmp;
            }
            catch (OperationCanceledException)
            {
                // пользователь отменил
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                OnPropertyChanged(nameof(Status));
            }
            finally
            {
                IsDownloading = false;
                _cts.Dispose();
            }
        }

        #endregion

        #region IDataErrorInfo

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (columnName != nameof(Url)) return null;
                if (string.IsNullOrWhiteSpace(Url)) return null;
                if (IsCheckingUrl) return null;
                if (!UrlFormatIsValid) return "Неверный формат или расширение";
                if (!UrlResourceIsValid) return ErrorMessage;
                return null;
            }
        }

        #endregion
    }
}
