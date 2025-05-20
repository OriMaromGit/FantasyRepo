using Newtonsoft.Json.Linq;

namespace FantasyNBA.Utils
{
    public class Utils
    {
        public static Dictionary<string, object> MergeDictionaries(List<Dictionary<string, object>> dictionaries)
        {
            var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var dict in dictionaries)
            {
                foreach (var (key, value) in dict)
                {
                    if (!result.ContainsKey(key))
                    {
                        result[key] = value;
                    }
                    else
                    {
                        result[key] = MergeValues(result[key], value);
                    }
                }
            }

            return result;
        }

        public static object MergeValues(object existingValue, object newValue)
        {
            if (existingValue is JObject existingJObject && newValue is JObject newJObject)
            {
                var existingDict = existingJObject.ToObject<Dictionary<string, object>>();
                var newDict = newJObject.ToObject<Dictionary<string, object>>();
                return MergeDictionaries(new List<Dictionary<string, object>> { existingDict, newDict });
            }

            if (existingValue is Dictionary<string, object> existingDictObj && newValue is Dictionary<string, object> newDictObj)
            {
                return MergeDictionaries(new List<Dictionary<string, object>> { existingDictObj, newDictObj });
            }

            // If not a mergeable object, default to the new value (override)
            return newValue;
        }

    }
}
