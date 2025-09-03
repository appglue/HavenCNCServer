using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;

namespace HavenCNCServer
{
    public class ApiStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "HavenCNC Server API",
                    Version = "v1",
                    Description = "REST API for CNC server management hosted within WinForms application",
                    Contact = new OpenApiContact
                    {
                        Name = "HavenCNC Server Team"
                    }
                });

                // Include XML comments for better documentation
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            // Add CORS for React frontend
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Add SPA services for React app
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "wwwroot";
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "HavenCNC Server API v1");
                c.RoutePrefix = "swagger";
            });

            app.UseRouting();
            app.UseCors();
            
            // Enable static files from wwwroot folder
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Configure SPA routing (fallback to index.html for React routing)
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
                
                // Fallback for client-side routing - return index.html for non-API routes
                spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                        Path.Combine(env.ContentRootPath, "wwwroot"))
                };
            });
        }
    }
}
