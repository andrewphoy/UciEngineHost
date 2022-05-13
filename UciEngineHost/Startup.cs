using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UciEngineHost {
    internal class Startup {

        public void ConfigureServices(IServiceCollection services) {
            services.AddSingleton<Configuration>();
            services.AddSingleton<Controller>();
            services.AddSingleton<HostApplicationContext>();
            services.AddSingleton<EngineHost>();
            services.AddSingleton<WebServer>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env) {
            app.UseWebSockets(new WebSocketOptions {
                KeepAliveInterval = TimeSpan.FromSeconds(300),
                ReceiveBufferSize = 4096
            });

            var server = app.ApplicationServices.GetRequiredService<WebServer>();
            app.Run(server.HandleRequest);
        }
    }
}
