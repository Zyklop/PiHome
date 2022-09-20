using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Communication.Networking;
using Coordinator.Modules;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PiUi.Services;

namespace PiUi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
	        services.AddHostedService<LoggingService>();
	        services.AddHostedService<NetworkSupervisor>();
	        services.AddHostedService<LanCommunicationService>();
            services.AddHostedService<PresetActivator>();
            services.AddTransient<ModuleFactory>();
            services.AddTransient<BroadcastConnector>();
            services.AddTransient<MulticastConnector>();
            services.AddTransient<LedController>();
            services.AddTransient<PresetRepository>();
            services.AddSingleton<MasterNetworker>();
            services.AddMvc(options => options.EnableEndpointRouting = false);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            
            app.UseStaticFiles();
            app.UseMvc();
        }
    }
}
