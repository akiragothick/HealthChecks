using HealthChecks.UI.Client;
using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Client.Monitor.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddHealthChecks();

            // services.AddHealthChecks()
            //         .AddCheck("AlwaysHealthy", () => HealthCheckResult.Healthy());
            //.AddCheck<MyCustomCheck>("My Custom Check");

            services.AddHealthChecks()
                    .AddCheck("AlwaysHealthy", () => HealthCheckResult.Healthy(), tags: new[] { "Tag1" })
                    .AddCheck("AlwaysHealthyToo", () => HealthCheckResult.Healthy(), tags: new[] { "Tag1" })
                    .AddCheck("AlwaysUnhealthy", () => HealthCheckResult.Unhealthy(), tags: new[] { "Tag2" });

            services.AddHealthChecks()
                    .AddSqlServer(Configuration["ConnectionString"]) // Your database connection string
                    .AddDiskStorageHealthCheck(s => s.AddDrive("C:\\", 1024)) // 1024 MB (1 GB) free minimum
                    .AddProcessAllocatedMemoryHealthCheck(512) // 512 MB max allocated memory
                    .AddProcessHealthCheck("ProcessName", p => p.Length > 0) // check if process is running
                    .AddWindowsServiceHealthCheck("someservice", s => s.Status == ServiceControllerStatus.Running)
                    .AddUrlGroup(new Uri("https://localhost:44318/weatherforecast"), "Example endpoint");


            services.AddHealthChecksUI(x =>
            {
                x.AddHealthCheckEndpoint("endpoint1", "http://localhost:44318/health1");
                x.AddHealthCheckEndpoint("endpoint2", "http://localhost:44318/health2");
                x.AddHealthCheckEndpoint("endpoint3", "http://localhost:44318/health3");
            })
            .AddInMemoryStorage();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true
                });

                endpoints.MapHealthChecks("/health1", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("Tag1"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/health2", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("Tag2"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                
            });
        }
    }
}
