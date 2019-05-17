using System;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using sqldb.shutt.re.Models;

namespace sqldb.shutt.re
{
    public static class PhotoDatabaseHelper
    {
        public static ulong? GetUserId(ClaimsPrincipal user)
        {
            var userIdStr = user?.Claims?.SingleOrDefault(x => x.Type == Models.User.ClaimType.UserId)?.Value;
            if (userIdStr == null)
            {
                return null;
            }
            try
            {
                return Convert.ToUInt64(userIdStr);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<User> RegisterNewUser(JwtSecurityToken securityToken)
        {
            await Task.CompletedTask;
            return null;
        }
    }
}