// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Vector3Converter.cs" company="ANN">
//     Copyright (c) ANN. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace MLPEnemyPos
{
    using System;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using SharpDX;

    public class Vector3Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var array = JArray.Load(reader);
            var vector = new Vector3
            {
                X = array[0].Value<int>(),
                Y = array[1].Value<int>(),
                Z = array[2].Value<int>()
            };

            return vector;
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            var vector = (Vector3)value;
            writer.WriteStartArray();
            writer.WriteValue(vector.X);
            writer.WriteValue(vector.Y);
            writer.WriteValue(vector.Z);
            writer.WriteEndArray();
        }
    }
}