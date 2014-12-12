using System;
using System.Data;

namespace nlog.jsonevent.layout.Test
{
    class TestDataTableGenerator
    {
        public DataTable Table;
        public TestDataTableGenerator()
        {
            Table = new DataTable("TestTable");
            Table.Columns.Add(new DataColumn("Id", typeof(int)));
            Table.Columns.Add(new DataColumn("Name", typeof(string)));
            Table.Columns.Add(new DataColumn("Location", typeof(string)));
            Table.Columns.Add(new DataColumn("Value", typeof(long)));
            Table.Columns.Add(new DataColumn("CreatedDate", typeof(DateTime)));

        }
        public void AddRow(int id, string name, string loc, long val, DateTime date)
        {
            var row = Table.NewRow();
            row["Id"] = id;
            row["Name"] = name;
            row["Location"] = loc;
            row["Value"] = val;
            row["CreatedDate"] = date;

            Table.Rows.Add(row);
        }
    }
}