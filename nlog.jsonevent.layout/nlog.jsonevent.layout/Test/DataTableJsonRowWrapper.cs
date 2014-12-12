using System.Data;

namespace nlog.jsonevent.layout.Test
{
    internal class DataTableJsonRowWrapper
    {
        public DataTable Data { get; set; }

        public static DataTableJsonRowWrapper Wrap(DataTable dt)
        {
            return new DataTableJsonRowWrapper
            {
                Data = dt
            };
        }
    }
}