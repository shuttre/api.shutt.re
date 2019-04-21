using System;
using System.Collections.Generic;
using System.IO;
using sqldb.shutt.re.Models;

namespace api.shutt.re
{
    public interface IImageHelper
    {
        string GetFullPath(string imagePath);
        CreateImageFilesResult CreateImageFiles(FileStream fileStream, string contentType, string fileHash);
    }
    
    public struct CreateImageFilesResult
    {
        public List<int> Sizes { get; set; }
        public Dictionary<int, ImageFile> Files { get; set; }
        public Image Image { set; get; }
        public AlbumImageMap AlbumImageMap { set; get; }
    }

}