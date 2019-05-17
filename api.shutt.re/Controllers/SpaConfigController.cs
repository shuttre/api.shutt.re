using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using sqldb.shutt.re;

namespace api.shutt.re.Controllers
{
    
    [ApiController]
    public class SpaConfigController : ControllerBase
    {
        private readonly IPhotoDatabase _pdb;

        public SpaConfigController(IPhotoDatabase pdb)
        {
            this._pdb = pdb;
        }

        [HttpGet]
        [Route("/spa")]
        public ActionResult<List<ApiDescription>> GetDescription()
        {
            var ret = new List<ApiDescription>()
            {
                new ApiDescription()
                {
                    Url = "GET /spa",
                    Arguments = ApiDescriptionArgument.Empty,
                    Comment = "Information about this api."
                },
                new ApiDescription()
                {
                    Url = "GET /spa/config",
                    Arguments = ApiDescriptionArgument.Empty,
                    PayloadDescription = ApiDescription.EmptyPayload,
                    Comment = "Get configuration data for SPA front-end."
                },                
            };
            return ret;
        }

        [HttpGet("/spa/config")]
        public async Task<ActionResult<SpaConfiguration>> GetConfig()
        {
            var config = await _pdb.GetConfig();
            await Task.CompletedTask;
            return new SpaConfiguration()
            {
                auth_endpoint = config.OidcAuthorizeEndpoint,
                token_endpoint = config.OidcTokenEndpoint,
                client_id = config.OidcClientId,
                aud = config.OidcAudience,
                callback = config.FrontendUrl + "/callback"
            };
        }
        
    }
}