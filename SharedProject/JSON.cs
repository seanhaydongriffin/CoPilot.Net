using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SharedProject
{
    public static class JSON
    {

        public static dynamic Deserialize(String json)
        {
            dynamic result = null;

            try
            {
                result = JsonConvert.DeserializeObject(json);
            }
            catch (Exception)
            {
            }

            return result;
        }

        public static string ToJSONString<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public static JObject Parse(String json)
        {
            JObject obj = null;

            try
            {
                JObject.Parse(json);
            } catch (Exception)
            {
            }

            return obj;
        }

        public static int ArrayCount(dynamic json_array)
        {
            JArray json_array_items = (JArray)json_array;
            int num_items = json_array_items.Count;
            return num_items;
        }

    }
}
