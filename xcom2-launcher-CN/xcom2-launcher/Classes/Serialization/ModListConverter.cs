﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XCOM2Launcher.Mod;
using XCOM2Launcher.XCOM;

namespace XCOM2Launcher.Serialization
{
    internal class ModListConverter : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType == typeof (Settings);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream
            var jObject = JObject.Load(reader);

            if (jObject["Arguments"]?.Type == JTokenType.Object)
            {
                // Transform Arguments object to string
                var defaultEnabledArgs = Argument.DefaultArguments.Where(arg => arg.IsEnabledByDefault).Select(arg => arg.Parameter).ToList().Aggregate((a, b) => a + " " + b);
                jObject["Arguments"] = new JValue(defaultEnabledArgs);
            }

            var settings = jObject.ToObject<Settings>();

            // repair old formats
            var modToken = jObject["Mods"];
            if (modToken == null)
                return settings;

            if (modToken["Entries"] == null)
                return settings;

            try
            {
                var i = 0;
                var mods = modToken.ToObject<Dictionary<string, List<ModEntry>>>();
                if (mods.Count > 0)
                {
                    foreach (var entry in mods)
                        foreach (var m in entry.Value)
                        {
                            m.Index = i++;
                            settings.Mods.AddMod(entry.Key, m);
                        }
                }
            }
            catch
            {
                // do nothing
            }


            return settings;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => JToken.FromObject(value).WriteTo(writer);
    }
}