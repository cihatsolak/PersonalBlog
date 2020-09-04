using AutoMapper;
using CSK.PersonalBlog.Business.Containers.MicrosoftIoC;
using CSK.PersonalBlog.Business.Tools.Settings;
using CSK.PersonalBlog.WebApi.CustomFilters;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Text;

namespace CSK.PersonalBlog.WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            //Cors Konfig�rasyonu : Api'nin canl�ya al�nmas�
            services.AddCors(options =>
            {
                options.AddPolicy("GlobalPolicy", cors =>
                {
                    cors.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            services.AddSwaggerDependencies(Configuration);
            services.AddExtensionDependencies(Configuration);

            services.AddAutoMapper(typeof(Startup));
            services.AddScoped(typeof(ValidateIdentifier<>));
            
            services.Configure<JwtSettings>(Configuration.GetSection(nameof(JwtSettings)));
            var jwtSettings = Configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; //Normal �artlarda https'i kapatmamam�z gereklidir fakat ssl sertifikam�z olmad��� i�in(localhost) kapat�yoruz.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecurityKey)),
                    ValidateLifetime = true, //�lgili token'�n zaman� bitti�i zaman ge�ersiz say�ls�n.
                    ValidateAudience = true, //benim belirtmi� oldu�um yerden gelmediyse bu token ge�ersiz anlam�na gelir.
                    ValidateIssuer = true, //benim belirtmi� oldu�um yerden gelmediyse bu token ge�ersiz anlam�na gelir.
                    ClockSkew = TimeSpan.Zero //Sunucular aras� zaman fark�n� engellemek i�in
                };
            });

            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            }).AddFluentValidation();
        }

        public void Configure(IApplicationBuilder app/*, IWebHostEnvironment env*/)
        {
            app.UseExceptionHandler("/Log/Error"); //Global hata yakalama.

            app.UseRouting();

            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            //Cors Config�rasyonu
            app.UseCors();

            //Swagger Setting
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/apidoc/swagger.json", "Blog Api");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
