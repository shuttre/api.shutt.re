using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using api.shutt.re.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // MySQL Connector/Net connection string
            // https://www.connectionstrings.com/mysql-connector-net-mysqlconnection/
            // Examples: 'Server=127.0.0.1;Database=my_db_name;Uid=my_username;Pwd=my_password;SslMode=none;'
            // Examples: 'Server=127.0.0.1;Database=my_db_name;Uid=my_username;Pwd=my_password;SslMode=Preferred;'
            // Examples: 'Server=127.0.0.1;Database=my_db_name;Uid=my_username;Pwd=my_password;SslMode=Required;'
            var connectionString = StaticConfiguration["MySqlConnectionString"];
            IPhotoDatabase pdb = new PhotoDatabase(connectionString);

            var config = pdb.GetConfig();
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
                    OnTokenValidated = context =>
                    {
                        if (context.SecurityToken is JwtSecurityToken securityToken)
                        {
                            var user = pdb.GetUserByOidcIdCached(securityToken.Subject).Result;
                            if (user == null) return Task.CompletedTask;
                            var claimsIdentity = (ClaimsIdentity)context.Principal.Identity;
                            foreach (var claim in user.GetClaims())
                            {
                                claimsIdentity.AddClaim(claim);
                            }
                        }
                        else
                        {
                            Console.WriteLine("It is not JwtSecurityToken");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddSingleton(pdb);
            services.AddSingleton(config);
            services.AddSingleton<IHostedService, HandleQueuedImagesService>();
            services.AddSingleton<IImageHelper, ImageHelper>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();
            app.UseMvc();
        }
    }
}