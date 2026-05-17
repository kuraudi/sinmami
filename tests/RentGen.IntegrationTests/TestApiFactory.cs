using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RentGen.IntegrationTests;

public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    public HttpClient CreateClientWithOverrides(IDictionary<string, string?> overrides)
    {
        var factory = WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(overrides);
            });
        });

        return factory.CreateClient();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var contentRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "RentGen.Api"));

        builder.UseContentRoot(contentRoot);
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Provider"] = "InMemory",
                ["Database:Name"] = $"rentgen-tests-{Guid.NewGuid()}",
                ["DeepSeek:ApiKey"] = string.Empty
            });
        });
    }
}
