using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Dapper;

namespace sqldb.shutt.re.Models
{
    public class User
    {
        public ulong UserId { get; }
        public string ProfileName { get; set; }
        public List<OidcProfile> OidcProfiles { get; set; }

        public User()
        {
        }

        public User(IEnumerable<UserResultRow> userResultRows)
        {
            OidcProfiles = new List<OidcProfile>();
            foreach (var row in userResultRows)
            {
                UserId = row.UserId;
                ProfileName = row.ProfileName;
                var p = new OidcProfile
                {
                    OidcProfileId = row.OidcProfileId,
                    OidcId = row.OidcId
                };
                OidcProfiles.Add(p);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("User id: " + UserId);
            sb.AppendLine(", Profile name: " + ProfileName);
            sb.AppendLine("OIDC Profiles:");
            foreach (var oidcProfile in OidcProfiles)
            {
                sb.Append("OIDC profile id:" + oidcProfile.OidcProfileId);
                sb.AppendLine(", OIDC id: " + oidcProfile.OidcId);
            }
            return sb.ToString();
        }

        public IEnumerable<Claim> GetClaims()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimType.UserId, UserId.ToString()), 
                new Claim(ClaimType.ProfileName, ProfileName)
            };
            foreach (var oidcP in OidcProfiles)
            {
                claims.Add(new Claim(ClaimType.OidcProfile, oidcP.OidcId));
            }
            return claims;
        }

        public static class ClaimType
        {
            public const string UserId = "shutt.re.userid";
            public const string ProfileName = "shutt.re.profile_name";
            public const string OidcProfile = "shutt.re.oidc_profile";
        }
    }

    public class OidcProfile
    {
        public ulong OidcProfileId { get; set; }
        public string OidcId { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class UserResultRow
    {
        public ulong UserId { get; set; }
        public string ProfileName { get; set; }
        public ulong OidcProfileId { get; set; }
        public string OidcId { get; set; }
    }

}
