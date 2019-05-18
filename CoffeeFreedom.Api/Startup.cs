using System.Collections.Generic;
using CoffeeFreedom.Api.Hubs;
using CoffeeFreedom.Api.Middleware;
using CoffeeFreedom.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace CoffeeFreedom.Api
{
    public class Startup
    {
        private readonly IConfiguration _config;

        public Startup(IConfiguration config)
        {
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddHttpContextAccessor();
            services.AddAuthentication("Basic")
                .AddScheme<BasicAuthenticationSchemeOptions, BasicAuthenticationScheme>("Basic", options =>
                    options.UserPasswords.Add(_config["WorkerUsername"], _config["WorkerPassword"]));
            services.AddSignalR();
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
            services.AddSingleton<WorkerService>();
            services.AddSingleton<IWorkPerformer>(serviceProvider => serviceProvider.GetRequiredService<WorkerService>());
            services.AddSingleton<IWorkerResponseHandler>(serviceProvider => serviceProvider.GetRequiredService<WorkerService>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMvc();
            app.UseAuthentication();
            app.UseSignalR(route => route.MapHub<CoffeeHub>("/coffeefreedom/hub"));
            app.UseSwagger(c => c.RouteTemplate = "coffeefreedom/swagger/{documentName}/swagger.json");
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "CoffeeFreedom v1");
                c.RoutePrefix = "coffeefreedom/swagger";
            });
        }
    }
}
