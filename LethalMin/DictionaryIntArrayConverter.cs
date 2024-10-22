using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace LethalMin
{
    public class DictionaryIntArrayConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = (Dictionary<int, int[]>)value;
            writer.WriteStartObject();
            foreach (var kvp in dictionary)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var dictionary = new Dictionary<int, int[]>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType == JsonToken.PropertyName)
                {
                    var key = int.Parse((string)reader.Value);
                    reader.Read();
                    var value = serializer.Deserialize<int[]>(reader);
                    dictionary.Add(key, value);
                }
            }
            return dictionary;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<int, int[]>);
        }
    }
}
