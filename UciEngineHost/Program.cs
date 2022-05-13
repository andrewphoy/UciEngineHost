using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace UciEngineHost {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) {
            if (!SingleInstance.Start()) {
                return;
            }
            try {
                ApplicationConfiguration.Initialize();

                var host = CreateWebHostBuilder(args).Build();
                ServiceProvider = host.Services;
                var config = ServiceProvider.GetRequiredService<Configuration>();
                config.Load();

                // run both the app context (win form) and kestrel (web server)
                host.RunAsync();
                var controller = ActivatorUtilities.CreateInstance<Controller>(ServiceProvider);
                Application.Run(controller.ApplicationContext);

            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "UCI Engine Host Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            SingleInstance.Stop();
        }

        public static IServiceProvider ServiceProvider { get; private set; }

        static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options => {
                    options.ListenLocalhost(6464, builder => {
                        builder.UseHttps();
                    });
                })
                .UseStartup<Startup>();
        
    }
}