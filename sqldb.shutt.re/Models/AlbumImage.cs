using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace sqldb.shutt.re.Models
{
    public class AlbumImage
    {

        public ulong AlbumId { get; set; } 
        public ulong ImageId { get; set; }
        public ulong ImageFileId => ImageFiles.FullsizeImageFile.ImageFileId;
        public string AlbumName { get; set; }
        public string Path => ImageFiles.FullsizeImageFile.Path;
        public string MimeType => ImageFiles.FullsizeImageFile.MimeType;
        public int Width => ImageFiles.FullsizeImageFile.Width;
        public int Height => ImageFiles.FullsizeImageFile.Height;
        public string ImageName { get; set; }
        public string OriginalHash { get; set; }
        public string OriginalFileName { get; set; }
        public int Read { get; set; }
        public int ReadOriginal { get; set; }
        public int Write { get; set; }
        public int Share { get; set; }
        public int Admin { get; set; }
        public AlbumImageFiles ImageFiles { get; set; }
        public Public GetPublic()
        {
            return new Public()
            {
                AlbumId = AlbumId,
                ImageId = ImageId,
                AlbumName = AlbumName,
                ImageName = ImageName,
                OriginalFileName = OriginalFileName,
                Read = Read,
                ReadOriginal = ReadOriginal,
                Write = Write,
                Share = Share,
                Admin = Admin,
                ImageFiles = ImageFiles.GetPublic()
            };
        }

        public AlbumImage(List<DbRow> rows)
        {
            if (rows == null) throw new ArgumentNullException(nameof(rows), "rows can't be null.");
            if (rows.Count == 0) throw new ArgumentException("rows can't be empty.");

            var firstRow = rows.First();
            
            AlbumId = firstRow.AlbumId;
            ImageId = firstRow.ImageId;
            AlbumName = firstRow.AlbumName;
            ImageName = firstRow.ImageName;
            OriginalHash = firstRow.OriginalHash;
            OriginalFileName = firstRow.OriginalFileName;
            Read = firstRow.Read;
            ReadOriginal = firstRow.ReadOriginal;
            Write = firstRow.Write;
            Share = firstRow.Share;
            Admin = firstRow.Admin;

            var albumImageFiles = rows.ToDictionary(x => x.ImageFileId, row => new AlbumImageFile()
            {
                Path = row.Path,
                Width = row.Width,
                Height = row.Height,
                MimeType = row.MimeType,
                ImageFileId = row.ImageFileId
            });

            ImageFiles = new AlbumImageFiles()
            {
                IconImageFile = firstRow.IconImageFileId != 0 ? albumImageFiles[firstRow.IconImageFileId] : null,
                SmallImageFile = firstRow.SmallImageFileId != 0 ? albumImageFiles[firstRow.SmallImageFileId] : null,
                MediumImageFile = firstRow.MediumImageFileId != 0 ? albumImageFiles[firstRow.MediumImageFileId] : null,
                LargeImageFile = firstRow.LargeImageFileId != 0 ? albumImageFiles[firstRow.LargeImageFileId] : null,
                FullsizeImageFile = firstRow.FullsizeImageFileId != 0
                    ? albumImageFiles[firstRow.FullsizeImageFileId]
                    : null,
                OriginalImageFile = firstRow.OriginalImageFileId != 0
                    ? albumImageFiles[firstRow.OriginalImageFileId]
                    : null,
            };
        }

        public static IEnumerable<AlbumImage> GetAlbumImageList(IEnumerable<DbRow> dbRows)
        {
            return dbRows.GroupBy(x => x.ImageId).Select(x => new AlbumImage(x.ToList()));
        }

        public class DbRow
        {
            public ulong AlbumId { get; set; } 
            public ulong ImageId { get; set; }
            public ulong ImageFileId { get; set; }
            public string AlbumName { get; set; }
            public string Path { get; set; }
            public string MimeType { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public string ImageName { get; set; }
            public string OriginalHash { get; set; }
            public string OriginalFileName { get; set; }
            public int Read { get; set; }
            public int ReadOriginal { get; set; }
            public int Write { get; set; }
            public int Share { get; set; }
            public int Admin { get; set; }
            public ulong IconImageFileId { get; set; }
            public ulong SmallImageFileId { get; set; }
            public ulong MediumImageFileId { get; set; }
            public ulong LargeImageFileId { get; set; }
            public ulong FullsizeImageFileId { get; set; }
            public ulong OriginalImageFileId { get; set; }
        }

        public class Public
        {
            public ulong AlbumId { get; set; } 
            public ulong ImageId { get; set; }
            public string AlbumName { get; set; }
            public string MimeType => ImageFiles.FullsizeImageFile.MimeType;
            public int Width => ImageFiles.FullsizeImageFile.Width;
            public int Height => ImageFiles.FullsizeImageFile.Height;
            public string ImageName { get; set; }
            public string OriginalFileName { get; set; }
            public int Read { get; set; }
            public int ReadOriginal { get; set; }
            public int Write { get; set; }
            public int Share { get; set; }
            public int Admin { get; set; }
            public AlbumImageFiles.Public ImageFiles { get; set; }
        }

        public class AlbumImageFile
        {
            public ulong ImageFileId { get; set; }
            public string Path { get; set; }
            public string MimeType { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }

            public Public GetPublic()
            {
                return new Public()
                {
                    Width = Width,
                    Height = Height,
                    MimeType = MimeType
                };
            }

            public class Public
            {
                public string MimeType { get; set; }
                public int Width { get; set; }
                public int Height { get; set; }                
            }
        }

        public class AlbumImageFiles
        {
            public AlbumImageFile IconImageFile { get; set; }
            public AlbumImageFile SmallImageFile { get; set; }
            public AlbumImageFile MediumImageFile { get; set; }
            public AlbumImageFile LargeImageFile { get; set; }
            public AlbumImageFile FullsizeImageFile { get; set; }
            public AlbumImageFile OriginalImageFile { get; set; }

            public class Public
            {
                public AlbumImageFile.Public IconImageFile { get; set; }
                public AlbumImageFile.Public SmallImageFile { get; set; }
                public AlbumImageFile.Public MediumImageFile { get; set; }
                public AlbumImageFile.Public LargeImageFile { get; set; }
                public AlbumImageFile.Public FullsizeImageFile { get; set; }
                public AlbumImageFile.Public OriginalImageFile { get; set; }
            }

            public Public GetPublic()
            {
                return new Public()
                {
                    IconImageFile = IconImageFile.GetPublic(),
                    SmallImageFile = SmallImageFile.GetPublic(),
                    MediumImageFile = MediumImageFile.GetPublic(),
                    LargeImageFile = LargeImageFile.GetPublic(),
                    FullsizeImageFile = FullsizeImageFile.GetPublic(),
                    OriginalImageFile = OriginalImageFile.GetPublic()
                };
            }

            public AlbumImageFile GetImageFile(string size)
            {
                switch (size)
                {
                    case "icon":
                        return IconImageFile;
                    case "small":
                        return SmallImageFile;
                    case "medium":
                        return MediumImageFile;
                    case "large":
                        return LargeImageFile;
                    case "fullsize":
                        return FullsizeImageFile;
                    case "original":
                        return OriginalImageFile;
                    default:
                        return null;
                }
            }
        }

    }
    
}