using CoffeeFreedom.Api.Hubs;
using CoffeeFreedom.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;

namespace CoffeeFreedom
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
            services.AddSignalR();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddHttpContextAccessor();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "CoffeeFreedom", Version = "v1" });
                c.AddSecurityDefinition("Basic", new BasicAuthScheme
                {
                    Description = "Please enter your CafeIT credentials."
                });
                c.AddSecurityRequirement(new Dictionary<string, IEnumerable<string>> { ["Basic"] = new string[0] });
            });
            services.AddScoped<ICoffeeService, CoffeeService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseSignalR(route => route.MapHub<CoffeeHub>("/hub"));
            app.UseMvc();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoffeeFreedom v1"));
        }
    }
}
