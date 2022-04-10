

using Newtonsoft.Json;

namespace Com.Api.Sdk;

/// <summary>
/// json 转换器，主要是去掉decimal小数点后面多余的0
/// </summary>
public class JsonConverterDecimal : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsSubclassOf(typeof(decimal));
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.Value == null)
        {
            return null;
        }
        else if (reader.Value.ToString() == "0")
        {
            return 0m;
        }
        else
        {
            return (decimal)((double)reader.Value);
        }
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        double input = 0;
        if (value is decimal)
        {
            var d = (decimal)value;
            input = Convert.ToDouble(d);
        }
        else if (value is float)
        {
            var d = (float)value;
            input = Convert.ToDouble(d);
        }
        else if (value != null)
        {
            input = (double)value;
        }
        writer.WriteValue((decimal)input);
    }
}