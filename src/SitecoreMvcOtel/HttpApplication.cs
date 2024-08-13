using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sitecore.DependencyInjection;
using System;
using System.Diagnostics;

namespace SitecoreMvcOtel;

public class HttpApplication : Sitecore.Web.Application
{
    private TracerProvider _tracerProvider;
    private MeterProvider _metricProvider;

    public override void Application_Start(object sender, EventArgs args)
    {
        base.Application_Start(sender, args);

        var configuration = ServiceLocator.ServiceProvider.GetRequiredService<Configuration>();
        var resourceBuilder = ServiceLocator.ServiceProvider.GetRequiredService<ResourceBuilder>();
        var solrConnection = new Uri(System.Configuration.ConfigurationManager.ConnectionStrings["Solr.Search"].ConnectionString, UriKind.Absolute);

        _metricProvider = Sdk.CreateMeterProviderBuilder()
            .AddAspNetInstrumentation()
            .AddHttpClientInstrumentation()
            .AddProcessInstrumentation()
            .AddOtlpExporter(builder =>
            {
                builder.Endpoint = configuration.Exporter.EndpointUri;
                builder.Protocol = configuration.Exporter.Protocol;
            })
            .SetResourceBuilder(resourceBuilder)
            .Build();

        var traceBuilder = Sdk.CreateTracerProviderBuilder()
            .AddAspNetInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.EnrichWithHttpWebRequest = (activity, request) =>
                {
                    // rename solr activites
                    if (activity.Kind == ActivityKind.Client
                        && request.RequestUri.Scheme == solrConnection.Scheme
                        && request.RequestUri.Host == solrConnection.Host
                        && request.RequestUri.Port == solrConnection.Port)
                    {
                        activity.DisplayName = $"{activity.DisplayName} [solr]";
                    }
                };

                options.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    // rename sitecore cts tracking activities
                    if (activity.Kind == ActivityKind.Client && request.RequestUri.Host == "cts.cloud.sitecore.net")
                    {
                        activity.DisplayName = $"{activity.DisplayName} [sitecore tracking]";
                    }
                };
            })
            .AddOtlpExporter(builder =>
            {
                builder.Endpoint = configuration.Exporter.EndpointUri;
                builder.Protocol = configuration.Exporter.Protocol;
            })
            .SetResourceBuilder(resourceBuilder);

        if (configuration.Traces.UseAlwaysOnSampler)
        {
            traceBuilder.SetSampler<AlwaysOnSampler>();
        }

        if (configuration.Instrumentation.UseSqlClientInstrumentation)
        {
            traceBuilder.AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.SetDbStatementForStoredProcedure = true;
                options.EnableConnectionLevelAttributes = true;
                options.RecordException = true;
            });
        }

        _tracerProvider = traceBuilder.Build();
    }

    protected void Application_End(object sender, EventArgs e)
    {
        _tracerProvider?.Dispose();
        _metricProvider?.Dispose();
    }
}
