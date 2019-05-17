using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using sqldb.shutt.re;
using sqldb.shutt.re.Models;
using api.shutt.re.Models.RequestBody;
using Microsoft.Extensions.Hosting;

namespace api.shutt.re.BackgroundServices
{
    public class HandleQueuedImagesService : BackgroundService
    {
        private readonly IPhotoDatabase _pdb;
        private readonly IImageHelper _imageHelper;

        public HandleQueuedImagesService(IPhotoDatabase pdb, IImageHelper imageHelper)
        {
            _pdb = pdb;
            _imageHelper = imageHelper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stopSignal = false;
            while (!stoppingToken.IsCancellationRequested && !stopSignal)
            {
                stopSignal = !await DoStuff();
            }
        }

        private async Task<bool> DoStuff()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var queuedImages = await _pdb.GetImageQueueEntries(100);
            foreach (var queuedImage in queuedImages)
            {
                if (!await HandleQueuedImage(queuedImage))
                {
                    return false;
                }
            }
            return true;
        }

        // ReSharper disable once MemberCanBeMadeStatic.Local
        private async Task<bool> HandleQueuedImage(QueuedImage queuedImage)
        {

            if (queuedImage.Status != 1)
            {
                // Ignoring this entry in queue. If it's not completed, it will be handled by the cleanup worker.
                return true;
            }

            var (orgFile, orgFileContentType) = await Utils.GetFileStreamAndContentType(_pdb,
                queuedImage.UserId,
                queuedImage.Path);

            if (!Utils.ContentTypeIsImage(orgFileContentType))
            {
                // TODO: Implement this
                return true;
            }

            var fileHash = Utils.GetHashString(orgFile);

            var createImageFilesResult = _imageHelper.CreateImageFiles(orgFile, orgFileContentType, fileHash);

            createImageFilesResult.AlbumImageMap.AlbumId = queuedImage.AlbumId;

            var insertImageSuccess = await _pdb.AddImageToAlbum(
                createImageFilesResult.Image,
                createImageFilesResult.AlbumImageMap,
                createImageFilesResult.Sizes, 
                createImageFilesResult.Files,
                queuedImage.QueuedImageId);

            Console.WriteLine($"[Handle queuedImage (id: {queuedImage.QueuedImageId}, " +
                              $"success: {insertImageSuccess.ToString()})] userId: {queuedImage.UserId}, " +
                              $"albumId: {queuedImage.AlbumId}, " +
                              $"virtualPath: {Utils.Base64Decode(queuedImage.Path)}, realPath: {orgFile.Name}, " +
                              $"contentType: {orgFileContentType}, fileHash: {fileHash}");

            return true;
        }
    }
}