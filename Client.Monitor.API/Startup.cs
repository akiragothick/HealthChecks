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
                .AddProcessAllocatedMemoryHealthCheck(int.Parse(Configuration[$"HealthChecks:MemoryMegabytes:Size"]), name: Configuration[$"HealthChecks:MemoryMegabytes:Name"], tags: new[] { "Disk" });
            #endregion

            #region HTTP
            for (int i = 0; i < 100; i++)
            {
                var tag = Configuration[$"HealthHttpChecks:{i}:Tag"];

                if (string.IsNullOrEmpty(tag))
                    break;

                for (int a = 0; a < 100; a++)
                {
                    var urlUri = Configuration[$"HealthHttpChecks:{i}:Uris:{a}:Uri"];
                    var nameUri = Configuration[$"HealthHttpChecks:{i}:Uris:{a}:Name"];

                    if (string.IsNullOrEmpty(urlUri))
                        break;

                    services.AddHealthChecks()
                        .AddUrlGroup(new Uri(urlUri), nameUri, tags: new[] { tag });
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
            for (int i = 0; i < 100; i++)
            {
                var name = Configuration[$"HealthDiskChecks:{i}:Name"];
                var drive = Configuration[$"HealthDiskChecks:{i}:Drive"];
                var size = Configuration[$"HealthDiskChecks:{i}:Size"];

                if (string.IsNullOrEmpty(name))
                    break;

                services.AddHealthChecks()
                    .AddDiskStorageHealthCheck(s => s.AddDrive(drive, 1024 * int.Parse(size)), name, tags: new[] { "Disk" });
            }
            #endregion

            #region WINDOWS SERVICE
            for (int i = 0; i < 100; i++)
            {
                var name = Configuration[$"HealthWSChecks:{i}:Name"];
                var service = Configuration[$"HealthWSChecks:{i}:Service"];

                if (string.IsNullOrEmpty(name))
                    break;

                services.AddHealthChecks()
                    .AddWindowsServiceHealthCheck(service, s =>
                    {
                        try
                        {
                            if (s.Status == ServiceControllerStatus.Running)
                                return true;
                            else
                                return false;
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }, name, tags: new[] { "WindowService" });
            }
            #endregion

            #region PING
            for (int i = 0; i < 100; i++)
            {
                var name = Configuration[$"HealthPingChecks:{i}:Name"];
                var host = Configuration[$"HealthPingChecks:{i}:Host"];

                if (string.IsNullOrEmpty(name))
                    break;

                services.AddHealthChecks()
                    .AddPingHealthCheck(s => s.AddHost(host, 2000), name, HealthStatus.Degraded, tags: new[] { "Ping" });
            }
            #endregion

            services.AddHealthChecksUI(x =>
            {
                //x.AddHealthCheckEndpoint("ALL HTTP Check", $"{Configuration["HealthChecks:Uri"]}/health");

                for (int i = 0; i < 100; i++)
                {
                    var name = Configuration[$"HealthHttpChecks:{i}:Name"];
                    var tag = Configuration[$"HealthHttpChecks:{i}:Tag"];

                    if (string.IsNullOrEmpty(name))
                        break;

                    x.AddHealthCheckEndpoint(name, $"{Configuration["HealthChecks:Uri"]}/{tag}");
                }

                x.AddHealthCheckEndpoint("Databases Check", $"{Configuration["HealthChecks:Uri"]}/healthdb");
                x.AddHealthCheckEndpoint("Windows Service Check", $"{Configuration["HealthChecks:Uri"]}/healthwservice");
                x.AddHealthCheckEndpoint("Ping Check", $"{Configuration["HealthChecks:Uri"]}/healthping");
                x.AddHealthCheckEndpoint("Disk Check", $"{Configuration["HealthChecks:Uri"]}/healthdisk");
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
                    setup.ApiPath = "/hc";
                    setup.UIPath = "/grindelwald-ui";
                    setup.AsideMenuOpened = false;
                    setup.AddCustomStylesheet("wwwroot\\css\\dotnet.css");
                });

                //endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                //{
                //    Predicate = _ => true,
                //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                //});

                for (int i = 0; i < 100; i++)
                {
                    var tag = Configuration[$"HealthHttpChecks:{i}:Tag"];

                    if (string.IsNullOrEmpty(tag))
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

                endpoints.MapHealthChecks("/healthwservice", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("WindowService"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/healthping", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("Ping"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                endpoints.MapHealthChecks("/healthdisk", new HealthCheckOptions()
                {
                    Predicate = (check) => check.Tags.Contains("Disk"),
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

            });
        }
    }
}
