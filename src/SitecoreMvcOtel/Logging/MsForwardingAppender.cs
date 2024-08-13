using log4net.spi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitecore.DependencyInjection;
using System;

namespace SitecoreMvcOtel.Logging;

public class MsForwardingAppender : log4net.Appender.AppenderSkeleton
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly Func<object, Exception> _getLogExceptionFunc;
    public string Category { get; set; }

    public MsForwardingAppender()
    {
        _loggerFactory = ServiceLocator.ServiceProvider.GetRequiredService<ILoggerFactory>();
        _getLogExceptionFunc = VisibilityBypasser.Instance.GenerateFieldReadAccessor<Exception>(typeof(LoggingEvent), "m_thrownException");
    }

    protected override void Append(LoggingEvent loggingEvent)
    {
        if (loggingEvent == null)
        {
            return;
        }

        var categoryName = Category;

        if (string.IsNullOrEmpty(categoryName))
        {
            categoryName = Name;
        }

        var logger = _loggerFactory.CreateLogger(categoryName);
        var msLogLevel = ConvertLevel(loggingEvent.Level);
        var message = loggingEvent.MessageObject?.ToString();
        var exception = _getLogExceptionFunc.Invoke(loggingEvent);

        if (exception == null)
        {
            logger.Log(msLogLevel, message);
        }
        else
        {
            logger.Log(msLogLevel, exception, message);
        }
    }

    private LogLevel ConvertLevel(Level log4netLevel)
    {
        if (log4netLevel == Level.DEBUG)
        {
            return LogLevel.Debug;
        }

        if (log4netLevel == Level.INFO)
        {
            return LogLevel.Information;
        }

        if (log4netLevel == Level.WARN)
        {
            return LogLevel.Warning;
        }

        if (log4netLevel == Level.ERROR)
        {
            return LogLevel.Error;
        }

        if (log4netLevel == Level.FATAL)
        {
            return LogLevel.Critical;
        }

        return LogLevel.Information;
    }
}
