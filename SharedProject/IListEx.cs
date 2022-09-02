using SharedProject.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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

        //public static IList<ExecutionGroupScript> Clone(this IList<ExecutionGroupScript> list)
        //{
        //    var newlist = new List<ExecutionGroupScript>();

        //    foreach (var item in list)

        //        newlist.Add(item);

        //    return newlist;
        //}

        public static List<T> CloneList<T>(this List<T> oldList)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, oldList);
            stream.Position = 0;
            return (List<T>)formatter.Deserialize(stream);
        }

    }
}
