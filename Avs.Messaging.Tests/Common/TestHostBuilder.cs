using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Avs.Messaging.Tests.Common;

public static class TestHostBuilder
{
    public static IHostBuilder CreateTestHostBuilder(Action<IServiceCollection>? configureServices = null)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IMessageVerifier, MessageVerifier>();
                configureServices?.Invoke(services);
            });
    }
}