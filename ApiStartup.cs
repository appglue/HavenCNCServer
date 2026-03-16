using HavenCNCServer.Services;
using HavenCNCServer.Hubs;
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
    /// <summary>
    /// Startup configuration class for the embedded ASP.NET Core Web API
    /// </summary>
    public class ApiStartup
    {
        /// <summary>
        /// Configures the services for dependency injection
        /// </summary>
        /// <param name="services">The service collection to configure</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Configure JSON serialization to respect JsonPropertyName attributes
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use property names as-is
                });
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
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000") // React app ports
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials(); // Required for SignalR
                });
            });

            // Add SignalR with aggressive keep-alive settings for real-time DRO updates
            services.AddSignalR(options =>
            {
                // Keep-alive interval - server sends keep-alive messages to client
                // Set to 2 seconds to match client configuration (client expects message every 2s)
                options.KeepAliveInterval = TimeSpan.FromSeconds(2);

                // Client timeout - how long server waits before considering client disconnected
                // Set to 10 seconds (must be at least 2x KeepAliveInterval)
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(10);

                // Maximum message buffer size
                options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB

                // Enable detailed errors in development
                options.EnableDetailedErrors = true;
            });

            // Add SPA services for React app
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "wwwroot";
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="env">The web host environment</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Always show developer exception page - this is a local-only server application
            app.UseDeveloperExceptionPage();

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
                endpoints.MapHub<CNCMessageHub>("/cncHub");
            });

            // Configure SPA routing with proper API route exclusion
            app.MapWhen(context => !context.Request.Path.StartsWithSegments("/api") &&
                                  !context.Request.Path.StartsWithSegments("/swagger"),
                appBranch =>
                {
                    appBranch.UseSpa(spa =>
                    {
                        spa.Options.SourcePath = "wwwroot";
                        spa.Options.DefaultPage = "/index.html";

                        spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
                        {
                            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                                Path.Combine(env.ContentRootPath, "wwwroot"))
                        };
                    });
                });
        }
    }
}
