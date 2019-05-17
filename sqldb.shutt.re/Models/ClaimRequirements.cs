using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Newtonsoft.Json;

namespace sqldb.shutt.re.Models
{
    public class ClaimRequirements
    {
        private List<ClaimRequirement> Requirements { get; set; }

        public ClaimRequirements(string json)
        {
            Requirements = JsonConvert.DeserializeObject<List<ClaimRequirement>>(json);
        }

        public bool Verify(IEnumerable<Claim> securityTokenClaims)
        {
            var securityTokenClaimsDict = 
                securityTokenClaims.GroupBy(x => x.Type).ToDictionary(y => y.Key, y => y.Select(z => z.Value).ToList());
            var r = Requirements
                .All(requirement => 
                    requirement.Values.Any(value => securityTokenClaimsDict[requirement.Type].Contains(value))
                );
            return r;
        }

        private class ClaimRequirement
        {
            public string Type { get; set; }
            public List<string> Values { get; set; }
        }
    }

}