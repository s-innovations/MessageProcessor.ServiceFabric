//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.ApplicationInsights;
//using Microsoft.ApplicationInsights.DataContracts;
//using Microsoft.ApplicationInsights.Extensibility;
//using Serilog.Configuration;
//using Serilog.Core;
//using Serilog.Events;
//using Serilog.Sinks.ApplicationInsights;

//namespace Serilog
//{
//    /// <summary>
//    /// Adds the WriteTo.ApplicationInsights() extension method to <see cref="LoggerConfiguration"/>.
//    /// </summary>
//    public static class LoggerConfigurationApplicationInsightsExtensions
//    {
//        /// <summary>
//        /// Adds a sink that writes log events against Microsoft Application Insights for the provided <paramref name="instrumentationKey"/>.
//        /// </summary>
//        /// <param name="loggerConfiguration">The logger configuration.</param>
//        /// <param name="instrumentationKey">Required Application Insights instrumentation key.</param>
//        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
//        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
//        /// <returns>
//        /// Logger configuration, allowing configuration to continue.
//        /// </returns>
//        /// <exception cref="System.ArgumentNullException">loggerConfiguration</exception>
//        /// <exception cref="System.ArgumentOutOfRangeException">instrumentationKey;Cannot be empty or null.</exception>
//        public static LoggerConfiguration ApplicationInsights(
//            this LoggerSinkConfiguration loggerConfiguration,
//            string instrumentationKey,
//            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
//            IFormatProvider formatProvider = null)
//        {
//            if (loggerConfiguration == null) throw new ArgumentNullException("loggerConfiguration");
//            return loggerConfiguration.Sink(new ApplicationInsightsSink(CreateTelemetryClientFromInstrumentationkey(instrumentationKey), formatProvider), restrictedToMinimumLevel);
//        }

//        /// <summary>
//        /// Adds a sink that writes log events against Microsoft Application Insights using the provided <paramref name="configuration"/>.
//        /// </summary>
//        /// <param name="loggerConfiguration">The logger configuration.</param>
//        /// <param name="configuration">Required Application Insights configuration settings.</param>
//        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
//        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
//        /// <returns>
//        /// Logger configuration, allowing configuration to continue.
//        /// </returns>
//        /// <exception cref="System.ArgumentNullException">
//        /// loggerConfiguration
//        /// or
//        /// configuration
//        /// </exception>
//        public static LoggerConfiguration ApplicationInsights(
//            this LoggerSinkConfiguration loggerConfiguration,
//            TelemetryConfiguration configuration,
//            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
//            IFormatProvider formatProvider = null)
//        {
//            if (loggerConfiguration == null) throw new ArgumentNullException("loggerConfiguration");
//            if (configuration == null) throw new ArgumentNullException("configuration");

//            return loggerConfiguration.Sink(new ApplicationInsightsSink(CreateTelemetryClientFromConfiguration(configuration), formatProvider), restrictedToMinimumLevel);
//        }

//        /// <summary>
//        /// Adds a sink that writes log events against Microsoft Application Insights using the provided <paramref name="telemetryClient"/>.
//        /// </summary>
//        /// <param name="loggerConfiguration">The logger configuration.</param>
//        /// <param name="telemetryClient">The telemetry client.</param>
//        /// <param name="restrictedToMinimumLevel">The restricted to minimum level.</param>
//        /// <param name="formatProvider">The format provider.</param>
//        /// <returns></returns>
//        /// <exception cref="System.ArgumentNullException">
//        /// loggerConfiguration
//        /// or
//        /// configuration
//        /// </exception>
//        public static LoggerConfiguration ApplicationInsights(
//            this LoggerSinkConfiguration loggerConfiguration,
//            TelemetryClient telemetryClient,
//            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
//            IFormatProvider formatProvider = null)
//        {
//            if (loggerConfiguration == null) throw new ArgumentNullException("loggerConfiguration");
//            if (telemetryClient == null) throw new ArgumentNullException("telemetryClient");

//            return loggerConfiguration.Sink(new ApplicationInsightsSink(telemetryClient, formatProvider), restrictedToMinimumLevel);
//        }

//        /// <summary>
//        /// Creates the telemetry client from a provided <paramref name="instrumentationKey"/>.
//        /// </summary>
//        /// <param name="instrumentationKey">The instrumentation key.</param>
//        /// <returns></returns>
//        /// <exception cref="System.ArgumentOutOfRangeException">instrumentationKey;Cannot be empty or null.</exception>
//        private static TelemetryClient CreateTelemetryClientFromInstrumentationkey(string instrumentationKey = "")
//        {
//            var telemetryClient = new TelemetryClient();

//            if (string.IsNullOrWhiteSpace(instrumentationKey) == false)
//            {
//                telemetryClient.InstrumentationKey = instrumentationKey;
//            }

//            return telemetryClient;
//        }

//        /// <summary>
//        /// Creates the telemetry client from the provided <paramref name="telemetryConfiguration"/>.
//        /// </summary>
//        /// <param name="telemetryConfiguration">The telemetry configuration.</param>
//        /// <returns>A new <see cref="TelemetryClient"/> if a <paramref name="telemetryConfiguration"/> was provided, otherwise the <see cref="TelemetryConfiguration.Active"/>.</returns>
//        private static TelemetryClient CreateTelemetryClientFromConfiguration(TelemetryConfiguration telemetryConfiguration = null)
//        {
//            return new TelemetryClient(telemetryConfiguration ?? TelemetryConfiguration.Active);
//        }
//    }
//}
//namespace Serilog.Sinks.ApplicationInsights
//{
//    /// <summary>
//    /// Writes log events to a Microsoft Azure Application Insights account.
//    /// Inspired by their NLog Appender implementation.
//    /// </summary>
//    public class ApplicationInsightsSink : ILogEventSink
//    {
//        /// <summary>
//        /// The format provider
//        /// </summary>
//        private readonly IFormatProvider _formatProvider;

//        /// <summary>
//        /// Holds the actual Application Insights TelemetryClient that will be used for logging.
//        /// </summary>
//        private readonly TelemetryClient _telemetryClient;

//        /// <summary>
//        /// Construct a sink that saves logs to the Application Insights account.
//        /// </summary>
//        /// <param name="telemetryClient">Required Application Insights telemetryClient.</param>
//        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
//        /// <exception cref="ArgumentNullException">telemetryClient</exception>
//        /// <exception cref="System.ArgumentNullException">telemetryClient</exception>
//        public ApplicationInsightsSink(TelemetryClient telemetryClient, IFormatProvider formatProvider = null)
//        {
//            if (telemetryClient == null) throw new ArgumentNullException("telemetryClient");

//            _telemetryClient = telemetryClient;
//            _formatProvider = formatProvider;
//        }

//        #region Implementation of ILogEventSink

//        /// <summary>
//        /// Emit the provided log event to the sink.
//        /// </summary>
//        /// <param name="logEvent">The log event to write.</param>
//        public void Emit(LogEvent logEvent)
//        {
//            var renderedMessage = logEvent.RenderMessage(_formatProvider);

//            // take logEvent and use it for the corresponding ITelemetry counterpart
//            if (logEvent.Exception != null)
//            {
//                var exceptionTelemetry = new ExceptionTelemetry(logEvent.Exception)
//                {
//                    SeverityLevel = logEvent.Level.ToSeverityLevel(),
//                    HandledAt = ExceptionHandledAt.UserCode,
//                    Timestamp = logEvent.Timestamp
//                };

//                // write logEvent's .Properties to the AI one
//                ForwardLogEventPropertiesToTelemetryProperties(exceptionTelemetry, logEvent, renderedMessage);

//                _telemetryClient.TrackException(exceptionTelemetry);
//            }
//            else
//            {
//                var eventTelemetry = new EventTelemetry(logEvent.MessageTemplate.Text)
//                {
//                    Timestamp = logEvent.Timestamp
//                };

//                // write logEvent's .Properties to the AI one
//                ForwardLogEventPropertiesToTelemetryProperties(eventTelemetry, logEvent, renderedMessage);

//                _telemetryClient.TrackEvent(eventTelemetry);
//            }
//        }

//        /// <summary>
//        /// Forwards the log event properties to the provided <see cref="ISupportProperties" /> instance.
//        /// </summary>
//        /// <param name="telemetry">The telemetry.</param>
//        /// <param name="logEvent">The log event.</param>
//        /// <param name="renderedMessage">The rendered message.</param>
//        /// <returns></returns>
//        private void ForwardLogEventPropertiesToTelemetryProperties(ISupportProperties telemetry, LogEvent logEvent, string renderedMessage)
//        {
//            telemetry.Properties.Add("LogLevel", logEvent.Level.ToString());
//            telemetry.Properties.Add("RenderedMessage", renderedMessage);

//            foreach (var property in logEvent.Properties.Where(property => property.Value != null && !telemetry.Properties.ContainsKey(property.Key)))
//            {
//                telemetry.Properties.Add(property.Key, property.Value.ToString());
//            }
//        }

//        #endregion
//    }

//    /// <summary>
//    /// Extension Method(s) for <see cref="LogEventLevel"/> instances
//    /// </summary>
//    public static class LogEventLevelExtensions
//    {
//        /// <summary>
//        /// To the severity level.
//        /// </summary>
//        /// <param name="logEventLevel">The log event level.</param>
//        /// <returns></returns>
//        public static SeverityLevel? ToSeverityLevel(this LogEventLevel logEventLevel)
//        {
//            switch (logEventLevel)
//            {
//                case LogEventLevel.Verbose:
//                case LogEventLevel.Debug:
//                    return SeverityLevel.Verbose;
//                case LogEventLevel.Information:
//                    return SeverityLevel.Information;
//                case LogEventLevel.Warning:
//                    return SeverityLevel.Warning;
//                case LogEventLevel.Error:
//                case LogEventLevel.Fatal:
//                    return SeverityLevel.Error;

//            }

//            return null;
//        }
//    }
//}
