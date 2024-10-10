// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var serviceCollection = new ServiceCollection();
var hostBuilder = Host.CreateDefaultBuilder();
hostBuilder.ConfigureServices((builderContext, serviceCollection) =>
{
    serviceCollection.AddSingleton("Hello World");
    serviceCollection.AddSingleton(serviceCollection);
});
var host = hostBuilder.Build();
Console.WriteLine(host.Services.GetService<IServiceCollection>());
Console.WriteLine(host.Services.GetService<string>());
