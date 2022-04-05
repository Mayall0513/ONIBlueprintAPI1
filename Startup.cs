using BlueprintAPI.Policies;
using BlueprintAPI.DbContexts;
using BlueprintAPI.Middlewares;
using BlueprintAPI.Models;
using BlueprintAPI.Models.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BlueprintAPI {
    public class Startup {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services) {
            services.AddControllers();

            string issuer = Configuration.GetSection("JWTSettings")["ClaimsIssuer"];
            string audience = Configuration.GetSection("JWTSettings")["ClaimsAudience"];

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, configure => {
                configure.RequireHttpsMetadata = true;
                configure.ClaimsIssuer = issuer;
                configure.Audience = audience;
                configure.IncludeErrorDetails = false;

                configure.Events = new JwtBearerEvents() {
                    OnAuthenticationFailed = context => {
                        context.NoResult();
                        return context.Response.WriteGenericResponseAsync(401, "Unable to authenticate token, acquire new credentials!");
                    },

                    OnForbidden = context => {
                        context.NoResult();
                        return context.Response.WriteGenericResponseAsync(403, "Forbidden!");
                    }
                };

                configure.TokenValidationParameters = new TokenValidationParameters() {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration.GetSection("JWTSettings")["Secret"])),
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    RequireSignedTokens = true,

                    ValidIssuer = issuer,
                    ValidAudience = audience
                };
            });

            services.AddAuthorization();

            services.AddDbContextPool<BlueprintsDbContext>(loginModel => {
                loginModel.UseSqlServer(Configuration.GetConnectionString("Database"));
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<BlueprintsDbContext>(); 
            services.AddScoped<AUserRepository, UserRepository>();
            services.AddScoped<ABlueprintRepository, BlueprintRepository>();
            services.AddScoped<ACollectionRepository, CollectionRepository>();

            services.AddMvc(configure => {
                configure.EnableEndpointRouting = false;
                configure.Filters.Add(typeof(HandleValidationExceptionAttribute));
            })
                
            .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = new JsonLowercaseNamingPolicy());

            services.AddRazorPages();
            services.AddRouting();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            app.UseHsts();
            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseRequestLocalization();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware(typeof(CheckAccountMiddleware));

            app.UseEndpoints(configure => {
                configure.MapControllers();
            });
        } 
    }
}
