using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using sqldb.shutt.re;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace api.shutt.re
{
    public static class Utils
    {
        public static string Base64Encode(string plainText)
        {
            if (plainText == null)
            {
                return null;
            }

            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            if (base64EncodedData == null)
            {
                return null;
            }

            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static async Task<string> GetRealPath(
            IPhotoDatabase pdb,
            ulong userId,
            ulong imageSourceId,
            string encodedPath)
        {
            var imageSourcesForUser = await pdb.GetImageSourcesForUser(userId);
            var source = imageSourcesForUser?.FirstOrDefault(x => x.ImageSourceId == imageSourceId);
            if (source == null)
            {
                return null;
            }

            var absoluteVirtualPath = Path.GetFullPath(Path.DirectorySeparatorChar + Utils.Base64Decode(encodedPath));
            if (!absoluteVirtualPath.StartsWith(source.SourceNameAbsolute))
            {
                return null;
            }

            var realPath = $"/{source.Path}/" + absoluteVirtualPath.Substring(source.SourceName.Length + 2);
            realPath = Path.GetFullPath(realPath);
            return realPath;
        }

        public static async Task<List<string>> GetDirectoryContent(
            IPhotoDatabase pdb,
            ulong userId, 
            ulong imageSourceId, 
            string encodedPath)
        {
            var path = await GetRealPath(pdb, userId, imageSourceId, encodedPath);
            if (path == null)
            {
                return null;
            }

            if (!Directory.Exists(path))
            {
                return null;
            }

            var dirs = new List<string>();

            var mimeDetector = new FileExtensionContentTypeProvider();

            foreach (var fsEntry in Directory.EnumerateFileSystemEntries(path))
            {
                if (System.IO.File.Exists(fsEntry))
                {
                    var successfulMimeDetection = mimeDetector.TryGetContentType(fsEntry, out var contentType);
                    if (!successfulMimeDetection) continue;
                    if (ContentTypeIsImage(contentType))
                    {
                        dirs.Add(Path.GetFileName(fsEntry));
                    }
                    else if (ContentTypeIsVideo(contentType))
                    {
                        // TODO: Implement this
                    }
                }
                else if (System.IO.Directory.Exists(fsEntry))
                {
                    dirs.Add(Path.GetFileName(fsEntry) + Path.DirectorySeparatorChar);
                }
            }

            dirs.Sort((a, b) =>
            {
                var aIsDir = a.EndsWith(Path.DirectorySeparatorChar) ? 1 : 0;
                var bIsDir = b.EndsWith(Path.DirectorySeparatorChar) ? 1 : 0;
                return aIsDir != bIsDir
                    ? aIsDir.CompareTo(bIsDir)
                    : (string.Compare(a, b, StringComparison.CurrentCulture));
            });

            return dirs.Count == 0 ? null : dirs;
        }

        public static async Task<Tuple<FileStream, string>> GetFileStreamAndContentType(
            IPhotoDatabase pdb,
            ulong userId, 
            ulong imageSourceId, 
            string encodedPath)
        {
            var path = await GetRealPath(pdb, userId, imageSourceId, encodedPath);
            if (path == null)
            {
                return null;
            }

            if (!System.IO.File.Exists(path))
            {
                return null;
            }

            var mimeDetector = new FileExtensionContentTypeProvider();

            var successfulMimeDetection = mimeDetector.TryGetContentType(path, out var contentType);

            return successfulMimeDetection
                ? new Tuple<FileStream, string>(System.IO.File.OpenRead(path), contentType)
                : null;
        }

        public static bool ContentTypeIsImage(string contentType)
        {
            // TODO: Needs more work to include Photoshop project files etc.  
            return contentType.StartsWith("image/");
        }

        public static bool ContentTypeIsVideo(string contentType)
        {
            return contentType.StartsWith("video/");
        }
        
        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static byte[] GetHash(Stream inputStream)
        {
            HashAlgorithm algorithm = SHA256.Create();
            var ret = algorithm.ComputeHash(inputStream);
            inputStream.Position = 0;
            return ret;
        }

        public static string GetHashString(string inputString)
        {
            return BitConverter.ToString(GetHash(inputString)).Replace("-", "").ToLower();
        }

        public static string GetHashString(Stream inputStream)
        {
            return BitConverter.ToString(GetHash(inputStream)).Replace("-", "").ToLower();
        }
        
    }
}