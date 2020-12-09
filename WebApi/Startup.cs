using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WebApi.Data;
using WebApi.Filters;

namespace WebApi
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
            // Add Entity Framework
            services.AddDbContext<StoreDbContext>(x => x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            
            services.AddControllers();
            
            // Add customer tracing using MVC Resource Filters
            services.AddMvc(options =>
            {
                options.Filters.Add(new TracingResourceFilter());
            });

            // Add Swagger
            services.AddSwaggerGen();

            services.AddHttpClient();

            var loggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogInformation("Hello");

            // Add OpenTelemetry
            services.AddOpenTelemetryTracing((serviceProvider, tracerBuilder) =>
            {
                // Make the logger factory available to the dependency injection
                // container so that it may be injected into the OpenTelemetry Tracer.
                //var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                tracerBuilder
                    .SetSampler(new AlwaysOnSampler())
                    // Adds the New Relic Exporter loading settings from the appsettings.json
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("NewRelic:ServiceName")))
                    .AddNewRelicExporter(options =>
                    {
                        options.ApiKey = this.Configuration.GetValue<string>("NewRelic:ApiKey");
                    })
                    // Adds Zipkin Exporter settings
                    .AddZipkinExporter(o =>
                    {
                        o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                        o.ServiceName = "FruitStand";
                    })
                    // Add Jaeger Exporter settings
                    .AddJaegerExporter(o =>
                    {
                        o.AgentHost = "localhost";
                        o.AgentPort = 6831;
                    })
                    .AddSource("CustomTrace")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(opt => opt.SetTextCommandContent = true);

                //var logger = loggerFactory.CreateLogger<Program>();
                logger.LogInformation("Hello from {name} {price}.", "tomato", 2.99);
            });

            services.AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.MigrateAndSeedData(development: true);
            }
            else
            {
                app.MigrateAndSeedData(development: false);
            }

            // Add Swagger
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FruitStand API");

                // Serve Swagger UI at the app's root level
                c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
