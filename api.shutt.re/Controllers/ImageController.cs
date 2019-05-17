using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class ImageController : ControllerBase
    {
        private readonly IPhotoDatabase _pdb;
        private readonly IImageHelper _imageHelper;

        public ImageController(IPhotoDatabase pdb, IImageHelper imageHelper)
        {
            _pdb = pdb;
            _imageHelper = imageHelper;
        }

        [HttpGet]
        [Route("/image")]
        public ActionResult<List<ApiDescription>> GetDescription()
        {
            var ret = new List<ApiDescription>()
            {
                new ApiDescription()
                {
                    Url = "GET /image",
                    Arguments = ApiDescriptionArgument.Empty,
                    Comment = "Information about this api."
                },
                new ApiDescription()
                {
                    Url = "GET /image/list/{albumId}",
                    Arguments = new List<ApiDescriptionArgument>()
                    {
                        new ApiDescriptionArgument("albumId", "Id of the album to list.")
                    },
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "List all images in an album."
                },
                new ApiDescription()
                {
                    Url = "GET /image/{albumId}/{imageId}?size={size}",
                    Arguments = new List<ApiDescriptionArgument>()
                    {
                        new ApiDescriptionArgument("albumId", "Id of the album."),
                        new ApiDescriptionArgument("imageId", "Id of the image to get."),
                        new ApiDescriptionArgument("size", "Optional value to indicate which " +
                                                           "version of the image to download. Valid values are: " +
                                                           "metadata, icon, small, medium, large, fullsize, original")
                    },
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "Download image or image metadata."
                }
            };
            return ret;
        }

        [HttpGet("/image/list/{albumId}")]
        [Authorize]
        public async Task<ActionResult<List<AlbumImage.Public>>> ListImages(ulong albumId)
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var images =
                (await _pdb.GetImagesInAlbumByUserIdAndAlbumId(userId.GetValueOrDefault(), albumId)).AsList();
            
            if (images != null && images.Count > 0)
            {
                return images.Select(x => x.GetPublic()).ToList();
            }
            
            return new NoContentResult();
        }

        [HttpGet("/image/{albumId}/{imageId}")]
        [ResponseCache(Duration = 604800)]
        [Authorize]
        public async Task<ActionResult> GetImage(ulong albumId, ulong imageId, [FromQuery] string size = "metadata")
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var validSizeValues = new[] {"metadata", "icon", "small", "medium", "large", "fullsize", "original"};
            if (!validSizeValues.Contains(size))
            {
                return new BadRequestResult();
            }
            
            var image = await _pdb.GetImageByUserIdAlbumIdAndImageId(userId.GetValueOrDefault(), albumId, imageId);

            if (image == null) return new NoContentResult();

            if (size == "metadata")
            {
                return new OkObjectResult(image.GetPublic());
            }

            try
            {
                var albumImageFile = image.ImageFiles.GetImageFile(size);
                if (albumImageFile == null)
                {
                    return new NoContentResult();
                }

                var x = new FileStream(_imageHelper.GetFullPath(albumImageFile.Path), FileMode.Open);

                if (!x.CanRead) return new NotFoundResult();
                
                var fileExt = Path.GetExtension(albumImageFile.Path);
                var virtualFilename = $"image_{albumId}_{imageId}_{size}{fileExt}";
                
                Response.Headers.Add("X-Width", albumImageFile.Width.ToString());
                Response.Headers.Add("X-Height", albumImageFile.Height.ToString());
                return File(x, albumImageFile.MimeType, virtualFilename);
            }
            catch
            {
                return new NotFoundResult();
            }

        }
    }
}