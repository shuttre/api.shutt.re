namespace sqldb.shutt.re.Models
{
    public class ImageFile
    {
        public const string JpegMimeType = "image/jpeg";
        
        public ulong ImageFileId { set; get; } 
        public string Path { set; get; } 
        public string MimeType { set; get; } 
        public int Width { set; get; } 
        public int Height { set; get; } 
    }
}