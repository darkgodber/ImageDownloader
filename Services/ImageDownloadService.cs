using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDownloader.Services
{
    /// <summary>
    /// Скачивает картинку по URL, отдаёт в виде потока
    /// и выдает прогресс (0–100%) по загруженным байтам.
    /// </summary>
    public class ImageDownloadService : IImageDownloadService
    {
        // Можно вынести в DI, если захотим мокать в тестах
        private static readonly HttpClient _httpClient = new HttpClient();

        // Размер буфера при чтении потока
        private const int BufferSize = 256 * 1024;

        /// <summary>
        /// Скачивает изображение в память, шагам отдаёт прогресс,
        /// поддерживает отмену через CancellationToken.
        /// </summary>
        public async Task<Stream> DownloadImageAsync(
            string url,
            IProgress<double>? progress,
            CancellationToken cancellationToken)
        {
            // 1) Запрос заголовков
            using var response = await _httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            response.EnsureSuccessStatusCode();

            // 2) Общий размер (может быть null)
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            // 3) Поток тела ответа
            using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            // 4) Копируем в MemoryStream, считая прогресс
            var memoryStream = new MemoryStream();
            var buffer = new byte[BufferSize];
            long totalRead = 0;

            while (true)
            {
                var bytesRead = await networkStream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken);
                if (bytesRead == 0)
                    break;

                await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    double percent = totalRead * 100.0 / totalBytes;
                    progress?.Report(percent);
                }
            }

            // 5) Сброс позиции для чтения с начала
            memoryStream.Position = 0;

            // В явном виде отчитаем 100%, если у нас был заголовок
            if (totalBytes > 0)
                progress?.Report(100);

            return memoryStream;
        }
    }
}
