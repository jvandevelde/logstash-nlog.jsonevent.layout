using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Layouts;

namespace nlog.jsonevent.layout
{
    public class ElasticSearchLayout : Layout
    {
        protected override string GetFormattedMessage(LogEventInfo logEvent)
        {
            return JsonConvert.SerializeObject(logEvent);
        }
    }
}
