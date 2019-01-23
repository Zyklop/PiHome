using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace PiUi
{
    public class Program
    {
        public static int Main(string[] args)
        {
	        Log.Logger = new LoggerConfiguration()
		        .MinimumLevel.Debug()
		        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
		        .MinimumLevel.Override("System", LogEventLevel.Information)
				.Enrich.FromLogContext()
		        .WriteTo.ColoredConsole()
		        .WriteTo.RollingFile(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
			        "kestrel.log"), LogEventLevel.Information, fileSizeLimitBytes: 100000000, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(10))
		        .CreateLogger();

	        try
	        {
		        Log.Information("Starting web host");
		        BuildWebHost(args).Run();
		        return 0;
	        }
	        catch (Exception ex)
	        {
		        Log.Fatal(ex, "Host terminated unexpectedly");
		        return 1;
	        }
	        finally
	        {
		        Log.CloseAndFlush();
	        }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseKestrel()
            .UseUrls("http://0.0.0.0:5000")
            .UseStartup<Startup>()
            .UseWebRoot(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "wwwroot"))
            .UseSerilog()
            .Build();
    }
}
