using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace PiUi
{
    public class Program
    {
        public static int Main(string[] args)
        {

	        try
	        {
                Console.WriteLine("Starting web host");
		        BuildWebHost(args).Run();
		        return 0;
	        }
	        catch (Exception ex)
	        {
                Console.WriteLine($"Host terminated unexpectedly: {ex}");
		        return 1;
	        }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
            .UseKestrel()
            .UseUrls("http://0.0.0.0:5000")
            .UseStartup<Startup>()
            .UseWebRoot(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "wwwroot"))
            .Build();
    }
}
