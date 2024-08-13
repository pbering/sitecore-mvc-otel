using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sitecore.Configuration;
using Sitecore.DependencyInjection;
using System;
using System.Linq;

namespace SitecoreMvcOtel;

public class ServiceConfigurator : IServicesConfigurator
{
    public void Configure(IServiceCollection services)
    {
        // parse and add configuration
        Enum.TryParse<OpenTelemetry.Exporter.OtlpExportProtocol>(SafeGetSetting("SitecoreMvcOtel.Exporter.Protocol"), true, out var exporterProtocol);

        var configuration = new Configuration(SafeGetSetting("SitecoreMvcOtel.ServiceName"),
            new Exporter(new Uri(SafeGetSetting("SitecoreMvcOtel.Exporter.EndpointUri")), exporterProtocol),
            new Traces(bool.Parse(SafeGetSetting("SitecoreMvcOtel.Traces.UseAlwaysOnSampler"))),
            new Instrumentation(bool.Parse(SafeGetSetting("SitecoreMvcOtel.Instrumentation.UseSqlClientInstrumentation")))
        );

        services.AddSingleton(configuration);

        // build and add resouce builder
        var resourceBuilder = ResourceBuilder.CreateDefault().AddService(
            serviceName: configuration.ServiceName,
            serviceInstanceId: Environment.MachineName.ToLowerInvariant(),
            autoGenerateServiceInstanceId: false)
                .AddProcessRuntimeDetector();

        services.AddSingleton(resourceBuilder);

        // configure logging
        services.AddLogging(builder =>
        {
            builder.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;

                options.AddOtlpExporter(options =>
                {
                    options.Endpoint = configuration.Exporter.EndpointUri;
                    options.Protocol = configuration.Exporter.Protocol;
                });

                options.SetResourceBuilder(resourceBuilder);
            });

            // remove "Sitecore.Diagnostics.SitecoreLoggerProvider" so we can reverse the flow from "mslog -> log4net" to "log4net -> mslog"
            services.Remove(services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(ILoggerProvider)));
        });
    }

    private string SafeGetSetting(string name)
    {
        var node = ConfigReader.GetConfiguration().SelectSingleNode($"sitecore/settings/setting[@name='{name}']");

        if (node == null)
        {
            return null;
        }

        var attribute = node.Attributes["value"];

        if (attribute == null)
        {
            return null;
        }

        return attribute.Value;
    }
}
