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
        private readonly bool _locationInfo;

        private readonly string _hostname = Environment.MachineName;
        private DateTime _timestamp;
        private string[] _ndc;
        private IDictionary<string, string> _mdc;
        private IDictionary<string, string> _exceptionInformation;
        private const int Version = 1;

        private JObject _logstashEvent;

        public static string IsoDatetimeTimeZoneFormatWithMillis = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        
        public JsonEventLayoutV1(bool locationInfo = false)
        {
            _locationInfo = locationInfo;
        }

        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {
            _timestamp = logEvent.TimeStamp;
            _exceptionInformation = new SortedDictionary<string, string>();
            _mdc = logEvent.Properties.ToDictionary(k => k.Key.ToString(), v => v.Value.ToString());
            _ndc = NestedDiagnosticsContext.GetAllMessages();
            _logstashEvent = new JObject();
            
            _logstashEvent.Add("@version", new JValue(Version));
            AddEventData("@timestamp", _timestamp.ToString(IsoDatetimeTimeZoneFormatWithMillis));
            AddEventData("source_host", _hostname);
            AddEventData("message", logEvent.FormattedMessage);

            if (logEvent.Exception != null)
            {
                var exception = logEvent.Exception;

                _exceptionInformation.Add("exception_class", exception.GetType().Name);
                _exceptionInformation.Add("exception_message", exception.Message);
                _exceptionInformation.Add("stacktrace", exception.StackTrace);

                AddEventData("exception", _exceptionInformation);
            }

            if (_locationInfo)
            {
                BuildCallsiteInformation(logEvent);
            }

            AddEventData("logger_name", logEvent.LoggerName);
            AddEventData("mdc", _mdc);
            AddEventData("ndc", string.Join(" | ", _ndc));
            AddEventData("level", logEvent.Level.Name);

            return string.Format("{0}{1}", JsonConvert.SerializeObject(_logstashEvent, Formatting.Indented), Environment.NewLine);
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