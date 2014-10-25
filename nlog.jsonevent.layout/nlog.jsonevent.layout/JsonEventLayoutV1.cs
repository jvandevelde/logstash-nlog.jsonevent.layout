using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Internal;
using NLog.Layouts;

namespace nlog.jsonevent.layout
{
    public class JsonEventLayoutV1 : Layout, IUsesStackTrace
    {
        public const string ISO8601DatetimeTimeZoneFormatWithMillis = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
        private const int LogstashJsonEventVersion = 1;

        private readonly bool _includeCallsiteInfo;

        private string[] _ndc;
        private IDictionary<string, string> _mdc;
        private IDictionary<string, string> _exceptionInformation;
        private JObject _logstashEvent;

        public JsonEventLayoutV1(bool includeCallsiteInfo = false)
        {
            _includeCallsiteInfo = includeCallsiteInfo;
        }

        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {
            _exceptionInformation = new SortedDictionary<string, string>();
            _mdc = logEvent.Properties.ToDictionary(k => k.Key.ToString(), v => v.Value.ToString());
            _ndc = NestedDiagnosticsContext.GetAllMessages();
            _logstashEvent = new JObject();
            
            _logstashEvent.Add("@version", new JValue(LogstashJsonEventVersion));
            AddEventData("@timestamp", logEvent.TimeStamp.ToString(ISO8601DatetimeTimeZoneFormatWithMillis));
            AddEventData("source_host", Environment.MachineName);
            AddEventData("level", logEvent.Level.Name);
            AddEventData("logger_name", logEvent.LoggerName);
            AddEventData("message", logEvent.FormattedMessage);

            BuildExceptionInformation(logEvent);

            if (_includeCallsiteInfo)
            {
                BuildCallsiteInformation(logEvent);
            }

            AddEventData("mdc", _mdc);
            AddEventData("ndc", string.Join(" | ", _ndc));
            
            return string.Format("{0}{1}", JsonConvert.SerializeObject(_logstashEvent, Formatting.Indented), Environment.NewLine);
        }

        private void BuildExceptionInformation(LogEventInfo logEvent)
        {
            if (logEvent.Exception != null)
            {
                var exception = logEvent.Exception;

                _exceptionInformation.Add("exception_class", exception.GetType().Name);
                _exceptionInformation.Add("exception_message", exception.Message);
                _exceptionInformation.Add("stacktrace", exception.StackTrace);

                AddEventData("exception", _exceptionInformation);
            }
        }

        private void BuildCallsiteInformation(LogEventInfo logEvent)
        {
            var frame = logEvent.StackTrace != null
                ? logEvent.StackTrace.GetFrame(logEvent.UserStackFrameNumber)
                : null;
            if (frame == null) return;

            AddEventData("line_number", frame.GetFileLineNumber().ToString(CultureInfo.InvariantCulture));

            var method = frame.GetMethod();

            if (method.DeclaringType != null)
            {
                var className = method.DeclaringType.FullName;

                // NLog.UnitTests.LayoutRenderers.CallSiteTests+<>c__DisplayClassa
                if (className.Contains("+<>"))
                {
                    var index = className.IndexOf("+<>", StringComparison.Ordinal);
                    className = className.Substring(0, index);
                }

                AddEventData("class", className);
            }
            else
            {
                AddEventData("class", "<no type>");
            }

            // Clean up the function name if it is an anonymous delegate
            // <.ctor>b__0
            // <Main>b__2
            var methodName = method.Name;
            if (methodName.Contains("__") 
                && methodName.StartsWith("<") 
                && methodName.Contains(">"))
            {
                var startIndex = methodName.IndexOf('<') + 1;
                var endIndex = methodName.IndexOf('>');

                methodName = methodName.Substring(startIndex, endIndex - startIndex);
            }

            AddEventData("method", methodName);

            var fileName = frame.GetFileName();
            if (fileName != null)
            {
                AddEventData("file", fileName);
            }
        }

        private void AddEventData(string keyname, string keyval)
        {
            if (keyval != null)
            {
                _logstashEvent.Add(keyname, new JValue(keyval));
            }
            else
            {
                _logstashEvent.Add(keyname, string.Empty);
            }
        }

        private void AddEventData(string keyname, IEnumerable<KeyValuePair<string, string>> keyval)
        {
            var list = new JArray();
            foreach (var obj in keyval.Select(kvp => new JObject {{kvp.Key, kvp.Value}}))
            {
                list.Add(obj);
            }
      
            _logstashEvent.Add(keyname, list);
        }

        public StackTraceUsage StackTraceUsage
        {
            get
            {    
                return StackTraceUsage.Max;   
            }
        }
    }
}