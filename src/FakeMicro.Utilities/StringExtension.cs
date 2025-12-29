using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeMicro.Utilities
{
    public static class StringExtension
    {
        public static dynamic ToExpando(this JArray array)
        {
            List<ExpandoObject> obj = new List<ExpandoObject>();
            foreach (var a in array)
            {
                if (a is JObject)
                {
                    obj.Add(((JObject)a).ToObject<IDictionary<string, object>>().ToExpando());
                }
            }
            return obj;
        }
        public static ExpandoObject ToExpando(this IDictionary<string, object>? dictionary)
        {
            if (dictionary == null)
            {
                return new ExpandoObject();
            }
            
            var expando = new ExpandoObject();
            var expandoDic = (IDictionary<string, object>)expando;
            foreach (var kvp in dictionary)
            {
                if (kvp.Value is IDictionary<string, object>)
                {
                    var expandoValue = ((IDictionary<string, object>)kvp.Value).ToExpando();
                    expandoDic.Add(kvp.Key, expandoValue);
                }
                else if (kvp.Value is Newtonsoft.Json.Linq.JArray)
                {
                    var itemList = new List<object>();
                    foreach (var item in (ICollection)kvp.Value)
                    {
                        if (item is Newtonsoft.Json.Linq.JContainer)
                        {
                            var expandoItem = ((Newtonsoft.Json.Linq.JContainer)item).ToObject<IDictionary<string, object>>().ToExpando();
                            itemList.Add(expandoItem);
                        }
                        else
                        {
                            if (item is Newtonsoft.Json.Linq.JValue)
                                itemList.Add(((Newtonsoft.Json.Linq.JValue)item).Value);
                            else
                                itemList.Add(item);
                        }
                    }
                    expandoDic.Add(kvp.Key, itemList);
                }
                else if (kvp.Value is Newtonsoft.Json.Linq.JContainer)
                {
                    var expandoValue = ((Newtonsoft.Json.Linq.JContainer)kvp.Value).ToObject<IDictionary<string, object>>().ToExpando();
                    expandoDic.Add(kvp.Key, expandoValue);
                }
                else
                {
                    expandoDic.Add(kvp);
                }
            }
            return expando;
        }
    }
}