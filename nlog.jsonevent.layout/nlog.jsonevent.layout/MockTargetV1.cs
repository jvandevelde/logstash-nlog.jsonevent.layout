using System.Collections.Generic;
using NLog;
using NLog.Layouts;
using NLog.Targets;

namespace nlog.jsonevent.layout
{
    internal class MockTargetV1 : TargetWithLayout
    {
        private static readonly List<string> Messages = new List<string>();

        public MockTargetV1(Layout layout)
        {
            Layout = layout;
        }

        protected override void Write(LogEventInfo logEvent)
        {

            Messages.Add(Layout.Render(logEvent));
        }

        public void Close()
        {
            Messages.Clear();
        }

        public bool RequiresLayout()
        {
            return true;
        }

        public string[] GetMessages()
        {
            return Messages.ToArray();
        }

        public void Clear()
        {
            Messages.Clear();
        }
    }
}
