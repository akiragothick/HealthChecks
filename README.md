# DEV POST

Web api Health Checks created in .Net Core Cli

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Step 0

```bash
dotnet new sln

mkdir Client.Monitor.API

cd Client.Monitor.API
dotnet new webapi

cd..
dotnet sln HealthChecks.sln add Client.Monitor.API/Client.Monitor.API.csproj

```

### Step 1

```bash
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.System
dotnet add package AspNetCore.HealthChecks.Uris
``` 

```c#
using System.ServiceProcess;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public void ConfigureServices(IServiceCollection services)
{
    services.AddHealthChecks();

    services.AddHealthChecks()
        .AddCheck("AlwaysHealthy", () => HealthCheckResult.Healthy())

    services.AddHealthChecks()
        .AddCheck("AlwaysHealthy", () => HealthCheckResult.Healthy(), tags: new[] { "Tag1" })
        .AddCheck("AlwaysHealthyToo", () => HealthCheckResult.Healthy(), tags: new[] { "Tag1" })
        .AddCheck("AlwaysUnhealthy", () => HealthCheckResult.Unhealthy(), tags: new[] { "Tag2" });

    services.AddHealthChecks()
        .AddSqlServer(Configuration["ConnectionString"]) // Your database connection string
        .AddDiskStorageHealthCheck(s => s.AddDrive("C:\\", 1024)) // 1024 MB (1 GB) free minimum
        .AddProcessAllocatedMemoryHealthCheck(512) // 512 MB max allocated memory
        .AddProcessHealthCheck("ProcessName", p => p.Length > 0) // check if process is running
        .AddWindowsServiceHealthCheck("someservice", s => s.Status == ServiceControllerStatus.Running); // check if a windows service is running
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();

        endpoints.MapHealthChecks("/health", new HealthCheckOptions()
        {
            Predicate = _ => true
        });

        endpoints.MapHealthChecks("/health1", new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains("Tag1")
        });

        endpoints.MapHealthChecks("/health2", new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains("Tag2")
        });

    });
}
``` 

### Step 2 UI
```bash
dotnet add package AspNetCore.HealthChecks.UI --version 3.1.1-preview4
dotnet add package AspNetCore.HealthChecks.UI.Client --version 3.1.1-preview3
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage --version 3.1.1-preview3
```

```c#
public void ConfigureServices(IServiceCollection services)
{
    //...
    services.AddHealthChecksUI(s =>
    {
        s.AddHealthCheckEndpoint("endpoint1", "https://localhost:44318/health");
    })
    .AddInMemoryStorage();
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // ...
    pp.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        endpoints.MapHealthChecksUI();

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
    // ...
}      
```

Here's what you'll find when you visit the /health endpoint:
And if you visit /healthchecks-ui, which is the default URL for the UI:

## Authors

* **Ernesto Vargas** - *Initial work* - [AkiraGothick](https://github.com/akiragothick)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details 