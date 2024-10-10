using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graphs.Services;

public class HostService
{
    public IHost Create()
    {
        var hostBuilder = Host.CreateDefaultBuilder();
        hostBuilder.ConfigureAppConfiguration((hostBuilderContext, configBuilder) => {
            
        });
        hostBuilder.ConfigureHostConfiguration((configBuilder) => {
        });
        hostBuilder.ConfigureContainer<object>((hostBuilderContext, b) => { 
        });
        var host = hostBuilder.Build();
        return host;
    }
}
