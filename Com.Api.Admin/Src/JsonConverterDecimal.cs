

using Newtonsoft.Json;

namespace Com.Api.Admin;

/// <summary>
/// json 转换器，主要是去掉decimal小数点后面多余的0
/// </summary>
public class JsonConverterDecimal : JsonConverter<decimal>
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="hasExistingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public override decimal ReadJson(JsonReader reader, Type objectType, decimal existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return existingValue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
    {
        double input = Convert.ToDouble(value);
        writer.WriteValue((decimal)input);
    }
}