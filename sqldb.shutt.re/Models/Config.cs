using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace sqldb.shutt.re.Models
{
    public class Config
    {
        private readonly Dictionary<string, string> _configurations;
        
        public Config(IEnumerable<ConfigRow> rows)
        {
            _configurations = rows.ToDictionary(x => x.Key, x => x.Value);
        }

        public string DatabaseVersion => _configurations.GetValueOrDefault("database_version");
        public string OidcAudience => _configurations.GetValueOrDefault("oidc_audience");
        public string OidcVerificationMethod => _configurations.GetValueOrDefault( "oidc_verification_method");
        public bool OidcVerificationMethodIsAuthorityUrl => OidcVerificationMethod == "authority_url";
        public string OidcAuthorityUrl => _configurations.GetValueOrDefault( "oidc_authority_url");
        public string MagickNetTempDirectory => _configurations.GetValueOrDefault( "magick_net_temp_directory");
        public string FileStorageDirectory => _configurations.GetValueOrDefault( "file_storage_directory");
        public string OidcAuthorizeEndpoint => _configurations.GetValueOrDefault( "oidc_authorize_endpoint");
        public string OidcTokenEndpoint => _configurations.GetValueOrDefault( "oidc_token_endpoint");
        public string OidcClientId => _configurations.GetValueOrDefault( "oidc_client_id");
        public string FrontendUrl => _configurations.GetValueOrDefault( "frontend_url");
        public string ClaimRequirementsJson => _configurations.GetValueOrDefault( "claim_requirements");
        
        public ClaimRequirements ClaimRequirements => new ClaimRequirements(this.ClaimRequirementsJson);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ConfigRow
    {
        public string Key { get; set; }
        public string Value { get; set; }        
    }
}