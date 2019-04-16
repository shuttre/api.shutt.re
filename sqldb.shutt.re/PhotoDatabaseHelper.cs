using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

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
    }
}