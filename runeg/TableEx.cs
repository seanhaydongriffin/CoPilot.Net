using BetterConsoleTables;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace runeg
{
    public static class TableEx
    {

        public static void AddWriteRow(this Table table, params object[] values)
        {
            table.AddRow(values);
            var table_str_arr = table.ToString().Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);
            Console.WriteLine(table_str_arr[table_str_arr.Length - 2]);
            Debug.WriteLine(table_str_arr[table_str_arr.Length - 2]);
        }

    }
}
