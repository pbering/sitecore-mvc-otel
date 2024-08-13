using System;

namespace SitecoreMvcOtel;

internal record Configuration(string ServiceName, Exporter Exporter, Traces Traces, Instrumentation Instrumentation);

internal record Exporter(Uri EndpointUri, OpenTelemetry.Exporter.OtlpExportProtocol Protocol);

internal record Traces(bool UseAlwaysOnSampler);

internal record Instrumentation(bool UseSqlClientInstrumentation);
