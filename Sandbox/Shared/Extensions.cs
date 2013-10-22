using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Shared
{
    public static class Extensions
    {
        public static T Clone<T>(this T obj)
        {
            return JObject.FromObject(obj).ToObject<T>();
        }
    }
}
