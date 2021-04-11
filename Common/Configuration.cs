using Newtonsoft.Json;

using System.Collections.Generic;
using System.IO;

namespace Common
{
    public sealed class Configuration
    {
        private static readonly Configuration instance = new();

        private static readonly Dictionary<string, ValueAndType> keysValues = new();
        private static readonly Dictionary<string, ValueAndType> defaultKeysValues = new();

        public static Configuration Instance { get => instance; }

        static Configuration()
        {
            SetDefaultKeysValues();

            string fileContent = File.ReadAllText("conf/SDAV_conf.json");
            JsonTextReader jsonTextReader = new(new StringReader(fileContent));

            bool propertyFound = false;
            bool valueFound = false;
            string propertyName = "";
            object value = null;

            while (jsonTextReader.Read())
            {
                JsonToken valueType = JsonToken.Null;

                if (jsonTextReader.Value != null)
                {
                    valueType = jsonTextReader.TokenType;
                    switch (valueType)
                    {
                        case JsonToken.PropertyName:
                            propertyName = jsonTextReader.Value.ToString();
                            propertyFound = true;
                            break;
                        default:
                            value = jsonTextReader.Value;
                            valueFound = true;
                            break;
                    }
                }

                if (propertyFound && valueFound)
                {
                    keysValues.Add(propertyName, new ValueAndType { Value = value, Type = valueType });
                    propertyFound = false;
                    valueFound = false;
                }
            }
        }

        private Configuration() { }

        public static int GetProperty(string name)
        {
            bool found = false;
            int result = int.MinValue;
            if (keysValues.TryGetValue(name, out ValueAndType valueAndType))
            {
                if (valueAndType.Type == JsonToken.Integer)
                {
                    result = int.Parse(valueAndType.Value.ToString());
                    found = true;
                }
            }
            if (!found)
            {
                if (defaultKeysValues.TryGetValue(name, out valueAndType))
                    if (valueAndType.Type == JsonToken.Integer)
                    {
                        result = (int)valueAndType.Value;
                        found = true;
                    }
            }

            if (!found)
                throw new KeyNotFoundException();

            return result;
        }

        private static void SetDefaultKeysValues()
        {
            defaultKeysValues.Add("spectrumBarCount", new ValueAndType { Value = 1024, Type = JsonToken.Integer });
            defaultKeysValues.Add("minFrequency", new ValueAndType { Value = 20, Type = JsonToken.Integer });
            defaultKeysValues.Add("maxFrequency", new ValueAndType { Value = 20000, Type = JsonToken.Integer });
            defaultKeysValues.Add("spectrumBarGenerations", new ValueAndType { Value = 150, Type = JsonToken.Integer });
            defaultKeysValues.Add("starsPerGeneration", new ValueAndType { Value = 100, Type = JsonToken.Integer });
            defaultKeysValues.Add("spectrumBarGenerationMultiplier", new ValueAndType { Value = 2, Type = JsonToken.Integer });
        }

        private struct ValueAndType
        {
            public object Value;
            public JsonToken Type;
        }
    }
}
