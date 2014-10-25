using System;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NUnit.Framework;

namespace nlog.jsonevent.layout.Test
{
    [TestFixture]
    public class Tests
    {
        private static Logger _logger;
        private static MockTargetV1 _target;
        private static MockTargetV1 _userFieldsTarget;
        private static JsonEventLayoutV1 userFieldsLayout;
        //static  String userFieldsSingle = new String("field1:value1");
        //static  String userFieldsMulti = new String("field2:value2,field3:value3");
        //static  String userFieldsSingleProperty = new String("field1:propval1");

        private static string[] _logstashFields =
        {
            "message",
            "source_host",
            "@timestamp",
            "@version"
        };


        [TestFixtureSetUp]
        public static void SetupTestAppender()
        {

            var config = new LoggingConfiguration();
            _target = new MockTargetV1(new JsonEventLayoutV1(true));
            config.AddTarget("mock", _target);
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
            _target.Clear();
            _target.Close();
        }

        [Test]
        public void TestJsonEventLayoutExceptions()
        {
            var exceptionMessage = "shits on fire, yo";
            _logger.Fatal("uh-oh", new InvalidCastException(exceptionMessage));
            var message = _target.GetMessages()[0];
            var obj = JsonConvert.DeserializeObject(message);
            Console.WriteLine(message);
            
            //JSONObject jsonObject = (JSONObject) obj;
            //JSONObject exceptionInformation = (JSONObject) jsonObject.get("exception");

            //Assert.Equals("Exception class missing", "java.lang.IllegalArgumentException",
            //    exceptionInformation.get("exception_class"));
            //Assert.Equals("Exception exception message", exceptionMessage,
            //    exceptionInformation.get("exception_message"));
        }
    }
}