using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
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

            // Add OpenTelemetry
            services.AddOpenTelemetryTracing(configure => configure
                .SetSampler(new AlwaysOnSampler())
                .AddZipkinExporter(o =>
                {
                    o.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
                    o.ServiceName = "ObservabilityDemo";
                })
                .AddJaegerExporter(o =>
                {
                    o.AgentHost = "localhost";
                    o.AgentPort = 6831;
                })
                .AddSource("CustomTrace")
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddSqlClientInstrumentation(opt => opt.SetTextCommandContent = true)
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
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Observability Demo API");

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
