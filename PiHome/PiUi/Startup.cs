using Coordinator.Modules;
using DataPersistance.Models;
using DataPersistance.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
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
            services.AddDbContext<PiHomeContext>(options => {
                options.UseNpgsql(Configuration.GetConnectionString("Postgres"));
            });
            services.AddHostedService<LoggingService>();
            services.AddHostedService<PresetActivator>();
            services.AddTransient<ModuleFactory>();
            services.AddTransient<LedController>();
            services.AddTransient<PresetRepository>();
            services.AddTransient<ButtonRepository>();
            services.AddTransient<LogRepository>();
            services.AddMvc(options => options.EnableEndpointRouting = false).AddNewtonsoftJson();
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
