using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Valora.Api;

namespace Valora.Tests
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Clear existing configuration sources
                config.Sources.Clear();

                // Add test configuration
                config.AddJsonFile("appsettings.Test.json", optional: false);
                config.AddEnvironmentVariables();
            });
        }
    }
}