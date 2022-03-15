using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ExportToCSV.Helpers
{
    public static class JObjectHelper
    {
        /// <summary>
        /// Convert an object to JObject
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static JObject ConvertToJObject(this object input)
        {
            return JObject.Parse(input.ToString());
        }

        /// <summary>
        /// Convert an object to JArray
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static JArray ConvertToJArray(this object input)
        {
            return JArray.Parse(input.ToString());
        }

        /// <summary>
        /// Check if an object is JObject
        /// </summary>
        /// <param name="inputOb"></param>
        /// <returns></returns>
        public static bool IsJObject(this object inputOb)
        {
            try
            {
                ConvertToJObject(inputOb);
            }
            catch
            {
                return false; //Something went wrong, cannot return a JObj
            }
            return true;
        }

        /// <summary>
        /// Check if an object is JArray
        /// </summary>
        /// <param name="inputOb"></param>
        /// <returns></returns>
        public static bool IsJArray(this object inputOb)
        {
            try
            {
                ConvertToJArray(inputOb);
            }
            catch
            {
                return false; //Something went wrong, cannot return a JArray
            }
            return true;
        }

        /// <summary>
        /// Verify if 2 JObjects are equals
        /// </summary>
        /// <param name="jObj1"></param>
        /// <param name="jObj2"></param>
        /// <returns></returns>
        public static bool EqualsJObj(this JObject jObj1, JObject jObj2)
        {

            if (jObj1.ToString() == jObj2.ToString()) // eq to string => same order
            {
                return true;
            }

            // Add property Name and property Value into the dictionary
            var JObj1PropertiesToDict = new Dictionary<string, JToken>(); // InputToBeFound to dictionary
            foreach (JProperty property in jObj1.Properties())
            {
                JObj1PropertiesToDict.Add(property.Name, property.Value);
            }

            // Verify if each property exists in dictionary
            foreach (JProperty propertyJObj2 in jObj2.Properties())
            {
                if (JObj1PropertiesToDict.ContainsKey(propertyJObj2.Name))
                {
                    var searchItemPropertyValue = JObj1PropertiesToDict[propertyJObj2.Name];
                    JObj1PropertiesToDict.Remove(propertyJObj2.Name); // Remove elements that I've already looked for 

                    if (propertyJObj2.Value.Equals(searchItemPropertyValue))
                    {
                        continue;
                    }

                    if (!searchItemPropertyValue.IsJObject() || !(propertyJObj2.Value).IsJObject())
                    {
                        return false;
                    }

                    var searchItemConverted = ConvertToJObject(searchItemPropertyValue);
                    var propertyConverted = ConvertToJObject(propertyJObj2.Value);
                    if (!searchItemConverted.EqualsJObj(propertyConverted))
                    {
                        return false;
                    }
                }
                else if (!string.IsNullOrEmpty(propertyJObj2.Value.ToString()))
                {
                    return false;
                }
            }

            // Could be more values in InputToBeFound than in Input list
            if (JObj1PropertiesToDict.Count > 0)
            {
                var JObj1PropertiesToDictValues = JObj1PropertiesToDict.Values;
                foreach (var value in JObj1PropertiesToDictValues)
                {
                    if (!string.IsNullOrEmpty(value.ToString()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        public static Dictionary<string, object> DeserializeAndFlatten(this string json)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            JToken token = JToken.Parse(json);
            FillDictionaryFromJToken(dict, token, "");
            return dict;
        }

        private static void FillDictionaryFromJToken(Dictionary<string, object> dict, JToken token, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (JProperty prop in token.Children<JProperty>())
                    {
                        FillDictionaryFromJToken(dict, prop.Value, Join(prefix, prop.Name));
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (JToken value in token.Children())
                    {
                        FillDictionaryFromJToken(dict, value, Join(prefix, index.ToString()));
                        index++;
                    }
                    break;

                default:
                    dict.Add(prefix, ((JValue)token).Value);
                    break;
            }
        }

        private static string Join(string prefix, string name)
        {
            return (string.IsNullOrEmpty(prefix) ? name : prefix + "." + name);
        }
    }
}