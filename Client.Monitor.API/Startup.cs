using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
//using System.Threading;
//using System.Threading.Tasks;

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

            //Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore.
            //  url/createdatabase.
            //services.AddHealthChecks().AddDbContextCheck<MyDbContext>();

            //services.AddDbContext<MyDbContext>(o => o.UseSqlServer(Configuration["ConnectionString"]));

            #region MEMORY HEALTH CHECK
            services.AddHealthChecks()
                .AddProcessAllocatedMemoryHealthCheck(1024, name: "1Mb allocated Healthy", tags: new[] { "Disk" });
            #endregion

            #region HTTP REST API
            for (int i = 0; i < 100; i++)
            {
                var urlBase = Configuration[$"HealthChecks:{i}:Base"];
                var tag = Configuration[$"HealthChecks:{i}:Tag"];

                if (string.IsNullOrEmpty(urlBase))
                    break;

                for (int a = 0; a < 100; a++)
                {
                    var urlUri = Configuration[$"HealthChecks:{i}:Uris:{a}:Uri"];
                    var nameUri = Configuration[$"HealthChecks:{i}:Uris:{a}:Name"];

                    if (string.IsNullOrEmpty(urlUri))
                        break;

                    services.AddHealthChecks()
                        .AddUrlGroup(new Uri($"{urlBase}{urlUri}"), nameUri, tags: new[] { tag });
                }
            }
            #endregion

            #region DATABASE
            for (int i = 0; i < 100; i++)
            {
                var nameDataBase = Configuration[$"HealthDBChecks:{i}:Name"];
                var connectionString = Configuration[$"HealthDBChecks:{i}:ConnectionString"];

                if (string.IsNullOrEmpty(nameDataBase))
                    break;

                services.AddHealthChecks()
                    .AddSqlServer(connectionString, name: nameDataBase, tags: new[] { "Databases" });
            }
            #endregion

            #region DISK
            services.AddHealthChecks()
                .AddDiskStorageHealthCheck(s => s.AddDrive("C:\\", 1024 * 90), name: "Disk C min 3Gb", tags: new[] { "Disk" })
                .AddDiskStorageHealthCheck(s => s.AddDrive("D:\\", 1024 * 3), name: "Disk D min 3Gb", tags: new[] { "Disk" });
            #endregion

            #region WINDOWS SERVICE
            for (int i = 0; i < 100; i++)
            {
                var name = Configuration[$"HealthWSChecks:{i}:Name"];
                var service = Configuration[$"HealthWSChecks:{i}:Service"];

                if (string.IsNullOrEmpty(name))
                    break;

                services.AddHealthChecks()
                    .AddWindowsServiceHealthCheck(service, s => s.Status == ServiceControllerStatus.Running, name, tags: new[] { "WindowService" });
            }
            #endregion

            //services.AddHealthChecks()
            //          .AddCheck("AlwaysHealthy", () => HealthCheckResult.Healthy(), tags: new[] { "Tag2" });


            services.AddHealthChecksUI(x =>
            {
                //x.AddHealthCheckEndpoint("ALL HTTP Check", "http://localhost:4081/health");

                for (int i = 0; i < 100; i++)
                {
                    var name = Configuration[$"HealthChecks:{i}:Name"];
                    var tag = Configuration[$"HealthChecks:{i}:Tag"];

                    if (string.IsNullOrEmpty(tag))
                        break;

                    x.AddHealthCheckEndpoint(name, $"http://localhost:4081/{tag}");
                }

                x.AddHealthCheckEndpoint("Databases Check", "http://localhost:4081/healthdb");
                x.AddHealthCheckEndpoint("Windows Service Check", "http://localhost:4081/healthwservice");
                x.AddHealthCheckEndpoint("Disk Check", "http://localhost:4081/healthdisk");                
            })
            .AddInMemoryStorage();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecksUI(setup =>
                {
                    setup.AddCustomStylesheet("wwwroot\\css\\dotnet.css");
                });

                //endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                //{
                //    Predicate = _ => true,
                //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                //});

                for (int i = 0; i < 100; i++)
                {
                    var urlBase = Configuration[$"HealthChecks:{i}:Base"];
                    var tag = Configuration[$"HealthChecks:{i}:Tag"];

                    if (string.IsNullOrEmpty(urlBase))
                        break;

                    endpoints.MapHealthChecks($"/{tag}", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Tags.Contains(tag),
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    });
                }

                endpoints.MapHealthChecks("/healthdb", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("Databases"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/healthdisk", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("Disk"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/healthwservice", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("WindowService"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }
    }
}
