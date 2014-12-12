using System;
using Nest;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;

namespace nlog.jsonevent.layout
{
    public class ElasticSearchTarget : Target
    {
        [RequiredParameter]
        public String NodeUrl { get; set; }

        [RequiredParameter]
        public Layout Layout { get; set; }

        private readonly ElasticClient _esClient;

        public ElasticSearchTarget(Layout layout, string nodeUrl)
        {
            var esNodeUrl = new Uri(nodeUrl);

            var esNodeSettings = new ConnectionSettings(esNodeUrl, "nlog.elasticsearch.target2");
            _esClient = new ElasticClient(esNodeSettings);

            Layout = layout;
        }

        protected override void Write(AsyncLogEventInfo info)
        {
            try
            {
               Index(info.LogEvent);
            }
            catch (Exception ex)
            {
                info.Continuation(ex);
            }
        }

        protected override void Write(AsyncLogEventInfo[] logEvents)
        {
            try
            {
                var bulkDescriptor = new BulkDescriptor();

                foreach (var logEvent in logEvents)
                {
                    bulkDescriptor.Index<LogEventInfo>(op => op.Document(logEvent.LogEvent));
                }
                
                _esClient.Bulk(bulkDescriptor);
            }
            catch (Exception ex)
            {
                InternalLogger.Error("Couldn't send log batch to ElasticSearch. {0}", ex.Message);
            }
        }

        protected override void Write(LogEventInfo logEvent)
        {
            Index(logEvent);
        }

        private void Index(LogEventInfo info)
        {
            try
            {
                _esClient.Index(info);
            }
            catch (Exception ex)
            {
                InternalLogger.Error("Couldn't send to ElasticSearch. {0}", ex.Message);
            
                throw;
            }
        }

        public void Close()
        {
            CloseTarget();
        }
    }
}