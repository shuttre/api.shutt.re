using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using sqldb.shutt.re.Models;
using Dapper;
using ImageMagick;
using Microsoft.Extensions.Configuration;

namespace api.shutt.re
{
    public class ImageHelper : IImageHelper
    {
        private readonly string _outputDirectory;
        
        public ImageHelper(Config config)
        {
            var imTemptDir = config.MagickNetTempDirectory;
            var outDir = config.FileStorageDirectory;
            SetMagickNetTempDir(imTemptDir?.Trim());
            _outputDirectory = CheckFileStorageDir(outDir?.Trim());
        }

        private static string CheckFileStorageDir(string outDir)
        {
            if (string.IsNullOrEmpty(outDir))
            {
                throw new InvalidOperationException($"You need to configure a FileStorageDirectory");
            }

            if (!Directory.Exists(outDir))
            {
                throw new InvalidOperationException($"Invalid value for FileStorageDirectory ({outDir}). Make " +
                                                    $"sure the folder exists, and is readable/writable.");
            }

            if (outDir.EndsWith("/"))
            {
                outDir = outDir.Substring(0, outDir.Length - 1);
            }

            return outDir;
        }

        private static void SetMagickNetTempDir(string imTemptDir)
        {
            if (string.IsNullOrEmpty(imTemptDir)) return;
            
            if (!Directory.Exists(imTemptDir))
            {
                throw new InvalidOperationException($"Can't call MagickNET.SetTempDirectory(...) " +
                                                    $"with ... = '{imTemptDir}'");
            }

            try
            {
                MagickNET.SetTempDirectory(imTemptDir);
            }
            catch (MagickException e)
            {
                Console.WriteLine($"Error when calling MagickNET.SetTempDirectory(...) " +
                                  $"with ... = '{imTemptDir}'", e);
                throw;
            }
        }

//        public string GetImagePath(string fullPath)
//        {
//            return (fullPath?.StartsWith(_outputDirectory)).GetValueOrDefault()
//                ? fullPath?.Substring(_outputDirectory.Length)
//                : null;
//        }

        public string GetFullPath(string imagePath)
        {
            return imagePath != null ? _outputDirectory + "/" + imagePath : null;
        }

        private string GetImagePath(string fileHash, int size, string extension = "jpg")
        {
            if (fileHash?.Length != 64)
            {
                return null;
            }

            var dirLevel1 = fileHash.Substring(0, 2);
            var dirLevel2 = fileHash.Substring(0, 4);
            var dirLevel3 = fileHash.Substring(0, 6);            

            return $"{dirLevel1}/{dirLevel2}/{dirLevel3}/{fileHash}_{size}.{extension ?? "unknown_extension"}";
        }

        public CreateImageFilesResult CreateImageFiles(FileStream fileStream, string contentType, string fileHash)
        {
            using (var image = new MagickImage(fileStream))
            {
                var ext = Path.GetExtension(fileStream.Name).Replace(".", "");
                var newFileName = GetImagePath(fileHash, 0, ext);

                var fullPath = GetFullPath(newFileName);
                var fullDir = Path.GetDirectoryName(fullPath);
                try
                {
                        
                    Directory.CreateDirectory(fullDir);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Can't create directory '{fullDir}'", e);
                    throw;
                }

                try
                {
                    File.Copy(fileStream.Name, fullPath, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Can't copy original file '{fileStream.Name}'", e);
                    throw;
                }

                var newFiles = new Dictionary<int, ImageFile>
                {
                    {
                        0, // size 0 represent original file
                        new ImageFile()
                        {
                            Path = newFileName, 
                            MimeType = contentType, 
                            Width = image.Width, 
                            Height = image.Height
                        }
                    }
                };

                var maxDim = Math.Max(image.Width, image.Height);
                // original (0), full size, large, medium, small, icon
                var sizes = new List<int>() { 0, maxDim, 3000, 1500, 800, 300 }
                    .Select(x => Math.Min(maxDim, x))
                    .ToList();

                image.AutoOrient();

                image.Format = MagickFormat.Jpeg;
                ClearExifDataKeepColorProfile(image);                
                
                foreach (var size in sizes.Distinct())
                {
                    if (size == 0) continue;
                    
                    newFileName = GetImagePath(fileHash, size);
                    fullPath = GetFullPath(newFileName);

                    var magickGeometry = new MagickGeometry(size, size);
                    image.Resize(magickGeometry);

                    try
                    {
                        image.Write(fullPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error when calling image.Write. fullPath: {fullPath}", e);
                        throw;
                    }

                    newFiles.Add(size, new ImageFile()
                    {
                        Path = newFileName,
                        MimeType = ImageFile.JpegMimeType,
                        Width = image.Width,
                        Height = image.Height
                    });

                }

                return new CreateImageFilesResult()
                {
                    Sizes = sizes.AsList(),
                    Files = newFiles,
                    Image = new Image()
                    {
                        OriginalHash = fileHash
                    },
                    AlbumImageMap = new AlbumImageMap()
                    {
                        OriginalFileName = Path.GetFileName(fileStream.Name) 
                    }
                };
            }
        }

        private static void ClearExifDataKeepColorProfile(IMagickImage image)
        {
            var cp = image.GetColorProfile();
            image.Strip();
            image.Comment = "";
            if (cp != null)
            {
                image.AddProfile(cp);
            }
        }
    }
}