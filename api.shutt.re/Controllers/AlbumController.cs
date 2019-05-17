using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sqldb.shutt.re;
using sqldb.shutt.re.Models;

namespace api.shutt.re.Controllers
{
    
    [ApiController]
    public class AlbumController : ControllerBase
    {
        private readonly IPhotoDatabase _pdb;

        public AlbumController(IPhotoDatabase pdb)
        {
            this._pdb = pdb;
        }

        [HttpGet]
        [Route("/album")]
        public ActionResult<List<ApiDescription>> GetDescription()
        {
            var ret = new List<ApiDescription>()
            {
                new ApiDescription()
                {
                    Url = "/album",
                    Arguments = ApiDescriptionArgument.Empty,
                    Comment = "Information about this api"
                },
                /*
                [HttpGet("/album/list")]
                [HttpGet("/album/list/{level}")]
                */
                new ApiDescription()
                {
                    Url = "GET /album/list",
                    Arguments = ApiDescriptionArgument.Empty,
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "Alias all albums you have read access to."
                },
                new ApiDescription()
                {
                    Url = "GET /album/list/[admin | write | share | read]",
                    Arguments = ApiDescriptionArgument.Empty,
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "List all albums you have [admin | write | share | read] access to"
                },
                new ApiDescription()
                {
                    Url = "GET /album/access_list/{albumId}",
                    Arguments = new List<ApiDescriptionArgument>()
                    {
                        new ApiDescriptionArgument("albumId", "id of album")
                    },
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "List all users that have access to an album. You must be an admin of the album to " +
                              "access the list."
                },
                new ApiDescription()
                {
                    Url = "POST /album/new/{albumName}",
                    Arguments = new List<ApiDescriptionArgument>()
                    {
                        new ApiDescriptionArgument("albumName", "Name of the album")
                    },
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "List all albums you have [admin | write | share | read] access to"
                },
                
            };
            return ret;
        }

        [HttpGet("/album/list")]
        [HttpGet("/album/list/{level}")]
        [Authorize]
        public async Task<ActionResult<List<Album>>> GetReadable(string level)
        {
            AlbumAccessLevel accessLevel;
            switch (level)
            {
                case "admin":
                    accessLevel = AlbumAccessLevel.Admin;
                    break;
                case "write":
                    accessLevel = AlbumAccessLevel.Write;
                    break;
                case "share":
                    accessLevel = AlbumAccessLevel.Share;
                    break;
                case "read":
                case "":
                case null:
                    accessLevel = AlbumAccessLevel.Read;
                    break;
                default:
                    return new NotFoundResult();
            }

            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var albums = (await _pdb.GetAlbumsAccessibleByUser(userId.GetValueOrDefault(), accessLevel))?.AsList();
            
            if (albums != null && albums.Count > 0)
            {
                return albums;
            }
            
            return new NoContentResult();
        }

        [HttpGet("/album/access_list/{albumId}")]
        [Authorize]
        public async Task<ActionResult<List<AlbumAccess>>> GetAccessList(ulong albumId)
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var accessList = (await _pdb.GetAlbumAccessByUserIdAndAlbumId(
                    userId.GetValueOrDefault(), 
                    albumId)
                )?.AsList();

            if (accessList != null && accessList.Count > 0)
            {
                return accessList;
            }

            return new NoContentResult();
        }
        
        [HttpPost("/album/new")]
        [Authorize]
        public async Task<ActionResult<Album>> PostNewAlbum([FromBody]Album newAlbum)
        {
            var userId = PhotoDatabaseHelper.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized();
            }

            var createdAlbum = await _pdb.CreateNewAlbum(userId.GetValueOrDefault(), newAlbum.AlbumName);

            if (createdAlbum != null)
            {
                return createdAlbum;
            }
            else
            {
                return new BadRequestResult();
            }
        }
        
    }
}