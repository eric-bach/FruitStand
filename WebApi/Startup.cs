using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Export;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
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

            #region OpenTelemetry Prometheus

            /*
            // Create and Setup Prometheus Exporter
            var promOptions = new PrometheusExporterOptions() { Url = $"http://localhost:{9184}/metrics/" };
            var promExporter = new PrometheusExporter(promOptions);
            var metricsHttpServer = new PrometheusExporterMetricsHttpServer(promExporter);
            metricsHttpServer.Start();
            
            // Create Processor (called Batcher in Metric spec, this is still not decided)
            var processor = new UngroupedBatcher();

            // Application which decides to enable OpenTelemetry metrics
            // would setup a MeterProvider and make it default.
            // All meters from this factory will be configured with the common processing pipeline.
            MeterProvider.SetDefault(Sdk.CreateMeterProviderBuilder()
                .SetProcessor(processor)
                .SetExporter(promExporter)
                .SetPushInterval(TimeSpan.FromSeconds(10))
                .Build());

            // The following shows how libraries would obtain a MeterProvider.
            // MeterProvider is the entry point, which provides Meter.
            // If user did not set the Default MeterProvider (shown in earlier lines),
            // all metric operations become no-ops.
            var meterProvider = MeterProvider.Default;
            var meter = meterProvider.GetMeter("MyMeter");

            // the rest is purely from Metrics API.
            var testCounter = meter.CreateInt64Counter("MyCounter");
            var testMeasure = meter.CreateInt64Measure("MyMeasure");
            var testObserver = meter.CreateInt64Observer("MyObservation", CallBackForMyObservation);
            var labels1 = new List<KeyValuePair<string, string>>();
            labels1.Add(new KeyValuePair<string, string>("dim1", "value1"));

            var labels2 = new List<KeyValuePair<string, string>>();
            labels2.Add(new KeyValuePair<string, string>("dim1", "value2"));
            var defaultContext = default(SpanContext);

            Stopwatch sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalMinutes < 5)
            {
                testCounter.Add(defaultContext, 100, meter.GetLabelSet(labels1));

                testMeasure.Record(defaultContext, 100, meter.GetLabelSet(labels1));
                testMeasure.Record(defaultContext, 500, meter.GetLabelSet(labels1));
                testMeasure.Record(defaultContext, 5, meter.GetLabelSet(labels1));
                testMeasure.Record(defaultContext, 750, meter.GetLabelSet(labels1));

                // Obviously there is no testObserver.Oberve() here, as Observer instruments
                // have callbacks that are called by the Meter automatically at each collection interval.

                Task.Delay(1000);
                var remaining = (5 * 60) - sw.Elapsed.TotalSeconds;
                System.Console.WriteLine("Running and emitting metrics. Remaining time:" + (int)remaining + " seconds");
            }
            */

            #endregion

            // Add OpenTelemetry
            services.AddOpenTelemetryTracing((serviceProvider, tracerBuilder) =>
            {
                tracerBuilder
                    .SetSampler(new AlwaysOnSampler())
                    // New Relic exporter settings
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(this.Configuration.GetValue<string>("NewRelic:ServiceName")))
                    .AddNewRelicExporter(options =>
                    {
                        options.ApiKey = this.Configuration.GetValue<string>("NewRelic:ApiKey");
                    })
                    // Zipkin exporter settings
                    .AddZipkinExporter(o =>
                    {
                        o.Endpoint = new Uri("http://192.168.1.44:9411/api/v2/spans");
                        o.ServiceName = "FruitStand";
                    })
                    // Jaeger exporter settings
                    .AddJaegerExporter(o =>
                    {
                        o.AgentHost = "192.168.1.44";
                        o.AgentPort = 6831;
                    })
                    // Custom tracing source
                    .AddSource("CustomTrace")
                    // OpenTelemetry instrumentation clients
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(opt => opt.SetTextCommandContent = true);
            });

            // Add ILogger for logging
            services.AddLogging();

            services.AddControllersWithViews()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
                );
        }

        /*
        internal static void CallBackForMyObservation(Int64ObserverMetric observerMetric)
        {
            var labels1 = new List<KeyValuePair<string, string>>();
            labels1.Add(new KeyValuePair<string, string>("dim1", "value1"));

            observerMetric.Observe(Process.GetCurrentProcess().WorkingSet64, labels1);
        }
        */

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            #region Add Promethes (without OpenTelemetry)

            // Custom Metrics to count requests for each endpoint and the method
            var counter = Metrics.CreateCounter("webapi_path_counter", "Counts requests to the Web API endpoints", new CounterConfiguration
            {
                LabelNames = new[] { "method", "endpoint" }
            });
            
            app.Use((context, next) =>
            {
                counter.WithLabels(context.Request.Method, context.Request.Path).Inc();
                return next();
            });
            
            // Use the Prometheus middleware
            app.UseMetricServer();
            app.UseHttpMetrics();

            #endregion

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // TODO This is a hack to ensure the Docker SQL server starts up before this executes
                System.Threading.Thread.Sleep(5000);

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
