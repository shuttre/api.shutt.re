using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using api.shutt.re.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using sqldb.shutt.re;
using sqldb.shutt.re.Models;

namespace api.shutt.re
{
    public class Startup
    {
        public Startup(IConfiguration staticConfiguration, IHostingEnvironment env)
        {
            StaticConfiguration = staticConfiguration;
        }

        private IConfiguration StaticConfiguration { get; }
        private readonly string _myCorsOriginsPolicy = "_myCorsOriginsPolicy";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // MySQL Connector/Net connection string
            // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/
            // Examples: 'Server=127.0.0.1;Database=my_db_name;Uid=my_username;Pwd=my_password;SslMode=none;'
            // Examples: 'Server=127.0.0.1;Database=my_db_name;Uid=my_username;Pwd=my_password;SslMode=Preferred;'
            // Examples: 'Server=127.0.0.1;Database=my_db_name;Uid=my_username;Pwd=my_password;SslMode=Required;'
            var connectionString = StaticConfiguration["SHUTTRE_CONNECTION_STRING"];
            IPhotoDatabase pdb = new PhotoDatabase(connectionString);

            var config = pdb.GetConfig().Result;           
            if (config == null)
            {
                Console.Error.WriteLine("Could not read configuration from database.");
                System.Environment.Exit(1);
            }
            if (!config.OidcVerificationMethodIsAuthorityUrl)
            {
                Console.Error.WriteLine("For now, 'authority_url' is the only supported verification method");
                System.Environment.Exit(1);
            }
            
            var claimRequirements = config.ClaimRequirements;

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {

                
                options.Authority = config.OidcAuthorityUrl;
                options.Audience = config.OidcAudience;
                options.Events = new JwtBearerEvents
                {                    
                    OnTokenValidated = async context =>
                    {
                        if (!(context.SecurityToken is JwtSecurityToken securityToken))
                        {
                            context.Fail("SecurityToken is not JwtSecurityToken");
                            return;
                        }
                        
                        if (!claimRequirements.Verify(securityToken.Claims))
                        {
                            context.Fail("claimRequirements.Verify failed");
                            return;
                        }

                        var user = await pdb.GetUserByOidcIdCached(securityToken.Subject);
                        if (user == null)
                        {
                            var newUser = await PhotoDatabaseHelper.RegisterNewUser(securityToken);
                            if (newUser == null)
                            {
                                context.Fail("User not in database, and could not create user.");
                                return;
                            }

                            user = newUser;
                        }
                            
                        var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
                        foreach (var claim in user.GetClaims())
                        {
                            claimsIdentity.AddClaim(claim);
                        }
                    }
                };
            });

            services.AddSingleton(pdb);
            services.AddSingleton(config);
            services.AddSingleton<IHostedService, HandleQueuedImagesService>();
            services.AddSingleton<IImageHelper, ImageHelper>();

            var frontendUurl = config.FrontendUrl;
            
            services.AddCors(options =>
            {
                options.AddPolicy(_myCorsOriginsPolicy,
                    builder =>
                    {
                        builder
                            .WithOrigins(frontendUurl)
                            .WithHeaders("Authorization", "Content-Type")
                            .WithMethods("GET", "POST")
                            .SetPreflightMaxAge(TimeSpan.FromSeconds(120));
                    });
            });
            
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();
            app.UseCors(_myCorsOriginsPolicy); 
            app.UseMvc();
        }
    }
}