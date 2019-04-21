using System.Collections.Generic;
using System.Threading.Tasks;
using api.shutt.re.Models.RequestBody;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sqldb.shutt.re;
using sqldb.shutt.re.Models;

namespace api.shutt.re.Controllers
{
    
    [ApiController]
    public class ImageSourceController : ControllerBase
    {
        private readonly IPhotoDatabase _pdb;

        public ImageSourceController(IPhotoDatabase pdb)
        {
            this._pdb = pdb;
        }

        [HttpGet]
        [Route("/source")]
        public ActionResult<List<ApiDescription>> GetDescription()
        {
            var ret = new List<ApiDescription>()
            {
                new ApiDescription()
                {
                    Url = "GET /source",
                    Arguments = ApiDescriptionArgument.Empty,
                    Comment = "Information about this api."
                },
                new ApiDescription()
                {
                    Url = "GET /source/list",
                    Arguments = ApiDescriptionArgument.Empty,
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "List all source directories."
                },                
                new ApiDescription()
                {
                    Url = "GET /source/dir?imageSourceId={imageSourceId}&path={encodedPath}",
                    Arguments = new List<ApiDescriptionArgument>()
                    {
                        new ApiDescriptionArgument("imageSourceId", "Id if the image source"),
                        new ApiDescriptionArgument("encodedPath", "Base64 encoded path of the directory " +
                                                           "to list")
                    },
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "List all files and directories in path."
                },                
                new ApiDescription()
                {
                    Url = "GET /source/file?imageSourceId={imageSourceId}&path={encodedPath}",
                    Arguments = new List<ApiDescriptionArgument>()
                    {
                        new ApiDescriptionArgument("imageSourceId", "Id if the image source"),
                        new ApiDescriptionArgument("encodedPath", "Base64 encoded path of the file " +
                                                                  "to download")
                    },
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "Downloads file from image source."
                },      
                new ApiDescription()
                {
                    Url = "POST /source/addToAlbum",
                    Arguments = ApiDescriptionArgument.Empty,
                    PayloadDescription = @"[{ 
                        imageSourceId: 1234,
                        albumId: 4321,
                        path: 'L05hbWUgb2YgaW1hZ2Ugc291cmNlL3NvbWUvc3ViL2ZvbGRlcnMvdmFjYXRpb24uanBn'
                    }, {...}, ...]",
                    Comment = "Queues an image to be added to an album."
                },      
                new ApiDescription()
                {
                    Url = "GET /source/queue",
                    Arguments = ApiDescriptionArgument.Empty,
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "Get all queued images for user"
                },      
            };
            return ret;
        }

        [HttpGet("/source/list")]
        [Authorize]
        public async Task<ActionResult<List<ImageSource>>> GetSources()
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return new NotFoundResult();
            }

            var sources = (await _pdb.GetImageSourcesForUser(userId.GetValueOrDefault()))?.AsList();
            
            if (sources != null && sources.Count > 0)
            {
                return sources;
            }
            
            return new NoContentResult();
        }

        [HttpGet("/source/dir")]
        [Authorize]
        public async Task<ActionResult<List<string>>> GetDirectory(
            [FromQuery] ulong imageSourceId, 
            [FromQuery] string path)
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return new NotFoundResult();
            }

            var dirContent = await Utils.GetDirectoryContent(_pdb, userId.GetValueOrDefault(), imageSourceId, path);

            if (dirContent == null)
            {
                return new NotFoundResult();
            }
            if (dirContent.Count == 0)
            {
                return new NoContentResult();
            }
            return dirContent;
        }

        [HttpGet("/source/file")]
        [Authorize]
        public async Task<ActionResult> GetFile(
            [FromQuery] ulong imageSourceId, 
            [FromQuery] string path)
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return new NotFoundResult();
            }

            var fileContent = await Utils.GetFileStreamAndContentType(_pdb, userId.GetValueOrDefault(), imageSourceId, path);

            if (fileContent?.Item1 == null || fileContent.Item2 == null)
            {
                return new NotFoundResult();
            }
            return File(fileContent.Item1, fileContent.Item2);
        }

        [HttpPost("/source/addToAlbum")]
        [Authorize]
        public async Task<ActionResult<List<QueuedImage>>> AddToAlbum([FromBody] List<QueuedImage> queuedImages)
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return new NotFoundResult();
            }

            foreach (var queuedImage in queuedImages)
            {
                queuedImage.Status = 0;
                var fileToQueue = await Utils.GetFileStreamAndContentType(_pdb, userId.GetValueOrDefault(), queuedImage.ImageSourceId, queuedImage.Path);
                // ReSharper disable once InvertIf
                if (fileToQueue == null)
                {
                    queuedImage.Status = 4;
                    queuedImage.StatusMsg = "File not found";
                }
            }
            
            var queuedImagesResult = (await _pdb.AddImagesToQueue(userId.GetValueOrDefault(), queuedImages));

            if (queuedImagesResult != null)
            {
                return queuedImagesResult;
            }
            
            return new NotFoundResult();
        }        

        [HttpGet("/source/queue")]
        [Authorize]
        public async Task<ActionResult<List<QueuedImage>>> GetQueue()
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return new NotFoundResult();
            }

            var queuedImages = (await _pdb.GetImageQueueForUser(userId.GetValueOrDefault()));

            if (queuedImages != null)
            {
                return queuedImages.AsList();
            }
            
            return new NotFoundResult();
        }
    }
}