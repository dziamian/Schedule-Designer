using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Converters;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Services;

namespace ScheduleDesigner
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
            services.AddDbContext<ScheduleDesignerDbContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
            });

            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new TimeSpanToStringConverter());
                });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                {
                    Version = "v1",
                    Title = "Schedule Designer API",
                    Description = "ASP.NET Core Web API for designing class schedule by many users simultaneously."
                });
            });

            services.AddScoped<ISettingsRepo, SqlSettingsRepo>();

            services.Configure<ApplicationInfo>(Configuration.GetSection("ApplicationInfo"));
            services.Configure<Consumer>(Configuration.GetSection("UsosConsumer"));

            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                {
                    builder
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins(Configuration.GetSection("CorsPolicy:AllowedOriginsList").Get<string[]>());
                });
            });

            services.AddSingleton<UsosAuthenticationService>();
            services
                .AddAuthentication("Usos")
                .AddScheme<UsosAuthenticationOptions, UsosAuthenticationHandler>("Usos", null);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("CorsPolicy");

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
