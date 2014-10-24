using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using NLog;
using NLog.Layouts;

namespace nlog.jsonevent.layout
{
    public class JsonEventLayoutV1 : Layout
    {
        private readonly bool _locationInfo;
        private readonly bool _ignoreThrowable;

        private readonly string _hostname = Environment.MachineName;
        private DateTime _timestamp;
        private string[] _ndc;
        private IDictionary<object, object> _mdc;
        private IDictionary<object, object> _gdc; 
        private StackFrame _info;
        private IDictionary<string, object> _exceptionInformation;
        private const int Version = 1;

        private Dictionary<string, object> _logstashEvent;

        public static string IsoDatetimeTimeZoneFormatWithMillis = "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'";
        public static string AdditionalDataProperty = "net.logstash.log4j.JSONEventLayoutV1.UserFields";
        private string _threadName;

        public JsonEventLayoutV1(bool ignoreThrowable, string threadName, string ndc) 
            : this(true, ignoreThrowable, threadName, ndc)
        {

        }

        public JsonEventLayoutV1(bool locationInfo, bool ignoreThrowable, string threadName, string ndc)
        {
            _locationInfo = locationInfo;
            _ignoreThrowable = ignoreThrowable;
            _threadName = threadName;
            _ndc = new[] {ndc};
        }

        protected override string GetFormattedMessage(LogEventInfo loggingEvent)
        {
            _timestamp = loggingEvent.TimeStamp;
            _exceptionInformation = new SortedDictionary<string, object>();
            _mdc = loggingEvent.Properties;
            _ndc = NestedDiagnosticsContext.GetAllMessages();
            _logstashEvent = new Dictionary<string, object>();

            AddEventData("@version", Version);
            AddEventData("@timestamp", _timestamp.ToString(IsoDatetimeTimeZoneFormatWithMillis));
            AddEventData("source_host", _hostname);
            AddEventData("message", loggingEvent.FormattedMessage);

            if (loggingEvent.Exception != null)
            {
                var throwableInformation = loggingEvent.Exception;
               
                _exceptionInformation.Add("exception_class", throwableInformation.GetType().Name);
                _exceptionInformation.Add("exception_message", throwableInformation.Message);
                _exceptionInformation.Add("stacktrace", throwableInformation.StackTrace);
                
                AddEventData("exception", _exceptionInformation);
            }

            if (loggingEvent.UserStackFrame != null)
            {
                _info = loggingEvent.UserStackFrame;
                AddEventData("file", _info.GetFileName());
                AddEventData("line_number", _info.GetFileLineNumber());
                AddEventData("class", _info.GetType().Name);
                AddEventData("method", _info.GetMethod());
            }

            AddEventData("logger_name", loggingEvent.LoggerName);
            AddEventData("mdc", _mdc);
            AddEventData("ndc", string.Join(" | ", _ndc));
            AddEventData("level", loggingEvent.Level);

            return string.Format("{0}{1}", JsonConvert.SerializeObject(_logstashEvent), Environment.NewLine);
        }

        private void AddUserFields(String data)
        {
            if (null != data)
            {
                var pairs = data.Split(new[] {','});
                foreach (var pair in pairs)
                {
                    var userField = pair.Split(new[] {':'}, 2);
                    if (userField[0] != null)
                    {
                        var key = userField[0];
                        var val = userField[1];
                        AddEventData(key, val);
                    }
                }
            }
        }

        private void AddEventData(string keyname, object keyval)
        {
            if (keyval != null)
            {
                _logstashEvent.Add(keyname, keyval);
            }
            else
            {
                _logstashEvent.Add(keyname, string.Empty);
            }
        }
    }
}