namespace sqldb.shutt.re.Models
{
    public class AlbumImageMap
    {
        public ulong AlbumId { set; get; } 
        public ulong ImageId { set; get; } 
        public string OriginalFileName { set; get; } 
        public string ImageName { set; get; } 
    }
}