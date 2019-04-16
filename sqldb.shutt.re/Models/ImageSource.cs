using System.IO;

namespace sqldb.shutt.re.Models
{
    public class ImageSource
    {
        public ulong ImageSourceId { get; set; }
        public ulong UserId { get; set; }
        public string Path { get; set; }
        public string SourceName { get; set; }

        public string SourceNameAbsolute
        {
            get
            {
                var prefix = SourceName.StartsWith(System.IO.Path.DirectorySeparatorChar)
                    ? ""
                    : System.IO.Path.DirectorySeparatorChar.ToString();
                var suffix = SourceName.EndsWith(System.IO.Path.DirectorySeparatorChar)
                    ? ""
                    : System.IO.Path.DirectorySeparatorChar.ToString();
                return prefix + SourceName + suffix;
            }
        }
        }
}