namespace sqldb.shutt.re.Models
{
    public class Image
    {
        public ulong ImageId { set; get; } 
        public ulong IconImageFileId { set; get; } 
        public ulong SmallImageFileId { set; get; } 
        public ulong MediumImageFileId { set; get; } 
        public ulong LargeImageFileId { set; get; } 
        public ulong FullSizeImageFileId { set; get; } 
        public ulong OriginalImageFileId { set; get; } 
        public string OriginalHash { set; get; } 
    }
}