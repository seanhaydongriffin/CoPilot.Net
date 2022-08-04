using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace SharedProject
{
    static class IListEx
    {
        public static IList Clone(this IList list)
        {
            var newlist = new List<object>();

            foreach (var item in list)

                newlist.Add(item);

            return newlist;
        }
    }
}
