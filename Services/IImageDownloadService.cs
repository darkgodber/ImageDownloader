using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageDownloader.Services
{
    public interface IImageDownloadService
    {
        Task<Stream> DownloadImageAsync(
            string url,
            IProgress<double> progress,
            CancellationToken cancellationToken);
    }
}
