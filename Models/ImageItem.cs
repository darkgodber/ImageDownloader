using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDownloader.Models
{
    /// <summary>
    /// Доменный объект для одной загрузки картинки:
    /// просто хранит URL и, скажем, байты или путь к файлу.
    /// </summary>
    public class ImageItem
    {
        public string Url { get; set; }
        public byte[] Data { get; set; }
    }
}

