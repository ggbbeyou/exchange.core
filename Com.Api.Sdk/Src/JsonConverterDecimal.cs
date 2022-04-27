

using Newtonsoft.Json;

namespace Com.Api.Sdk;

/// <summary>
/// json 转换器，主要是去掉decimal小数点后面多余的0
/// </summary>
public class JsonConverterDecimal : JsonConverter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="objectType"></param>
    /// <returns></returns>
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsSubclassOf(typeof(decimal));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
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