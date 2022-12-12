using System;
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
            .Build();
    }
}
