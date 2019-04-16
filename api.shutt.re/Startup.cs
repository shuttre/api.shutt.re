using System;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using api.shutt.re;
using api.shutt.re.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using sqldb.shutt.re;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace api.shutt.re
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment CurrentEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

//            var builder = new SqlConnectionStringBuilder(Configuration.GetConnectionString("sqldb.shutt.re"))
//            {
//                Password = Configuration["DbPassword"],
//            };
//            var connectionString = builder.ConnectionString;
            
//            var connectionString = Configuration.GetConnectionString("sqldb.shutt.re");
            
            var connectionString = Configuration["ConnectionStrings:sqldb.shutt.re"];
            
            var pdb = new PhotoDatabase(connectionString);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Authority = Configuration["oidc:Authority"];
                options.Audience = Configuration["oidc:Audience"];
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

            services.AddSingleton<IPhotoDatabase>(pdb);
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