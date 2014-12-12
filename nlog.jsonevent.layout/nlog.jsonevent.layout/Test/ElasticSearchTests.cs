using System;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NUnit.Framework;

namespace nlog.jsonevent.layout.Test
{
    [TestFixture]
    public class ElasticSearchTests
    {
        private static Logger _logger;
        //private static MockTargetV1 _target;
        private static ElasticSearchTarget _target;
        private static MockTargetV1 _userFieldsTargeton;
        private static ElasticSearchLayout userFieldsLayout;
        //static  String userFieldsSingle = new String("field1:value1");
        //static  String userFieldsMulti = new String("field2:value2,field3:value3");
        //static  String userFieldsSingleProperty = new String("field1:propval1");
        private static ElasticClient _esClient;

        [TestFixtureSetUp]
        public static void SetupTestAppender()
        {
            var esNodeUrl = new Uri(@"http://localhost:9200");

            var esNodeSettings = new ConnectionSettings(esNodeUrl, "nlog.elasticsearch.target");
            _esClient = new ElasticClient(esNodeSettings);

            var config = new LoggingConfiguration();
            var layout = new ElasticSearchLayout();
            //_target = new MockTargetV1(layout);
            _target = new ElasticSearchTarget(layout, @"http://localhost:9200")
            {
                NodeUrl = @"http://localhost:9200"
            };
            //config.AddTarget("mock", _target);
            config.AddTarget("es", _target);
            //_target.Layout = @"${message}";

            var rule = new LoggingRule("*", LogLevel.Debug, _target);

            config.LoggingRules.Add(rule);


            LogManager.Configuration = config;
            _logger = LogManager.GetCurrentClassLogger();
        }

        [TestFixtureTearDown]
        public void ClearTestAppender()
        {
            NestedDiagnosticsContext.Clear();
            // _target.Clear();
            _target.Close();
        }

        [Test]
        public void TestJsonEventLayoutExceptions()
        {
            var exceptionMessage = "shits on fire, yo";
            _logger.Fatal("uh-oh", new InvalidCastException(exceptionMessage));
            _logger.Debug("huh");
            _logger.Debug("huh1");
            _logger.Debug("huh2");
            _logger.Debug("huh3");

            //JSONObject jsonObject = (JSONObject) obj;
            //JSONObject exceptionInformation = (JSONObject) jsonObject.get("exception");

            //Assert.Equals("Exception class missing", "java.lang.IllegalArgumentException",
            //    exceptionInformation.get("exception_class"));
            //Assert.Equals("Exception exception message", exceptionMessage,
            //    exceptionInformation.get("exception_message"));
        }

        [Test]
        public void can_write_a_datatable_to_elasticsearch_target()
        {
            var gen = new TestDataTableGenerator();
            gen.AddRow(1, "testName", "Wpg", 99L, DateTime.Now);
            var dt = gen.Table;

            var json = JsonConvert.SerializeObject(DataTableJsonRowWrapper.Wrap(dt));

            //_esClient.Raw.Index("dtable", Table.TableName, json);
            _esClient.Raw.Index("dtable", dt.TableName +"raw", DataTableJsonRowWrapper.Wrap(dt));
        }

        // http://localhost:9200/_all/_mapping/
        // http://localhost:9200/_plugin/head/
        [Test]
        public void can_write_individual_datatable_rows_to_elasticsearch_target()
        {
            var gen = new TestDataTableGenerator();
            gen.AddRow(1, "testName", "Wpg", 99L, DateTime.Now);
            gen.AddRow(2, "testNamesdf", "Wpg", 132L, DateTime.Now.AddDays(-11));
            gen.AddRow(3, "testNamefgh", "Wpg", 12354L, DateTime.Now.AddDays(3));
            var dt = gen.Table;


            var bulkDescriptor = new BulkDescriptor();
            var json = JsonConvert.SerializeObject(dt);

            //http://weblog.west-wind.com/posts/2012/Aug/30/Using-JSONNET-for-dynamic-JSON-parsing
            //var jsonobj = JValue.Parse(json); // Causes additional metadata to be stored for each row
            var jsonobj = JArray.Parse(json);

            foreach (var row in jsonobj)
            {
                bulkDescriptor.Index<object>(op =>
                {
                    op.Type("singleRowType2");      // can control both index and type like this instead of by generics
                    op.Index("dtabletest44");
                    op.Document(row);

                    return op;
                });
            }

            _esClient.Bulk(bulkDescriptor);
        }
    }
}
