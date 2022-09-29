

using Newtonsoft.Json;

namespace Com.Bll.Util;

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
        if (reader.Value == null)
        {
            return 0;
        }
        if (reader.ValueType == typeof(string))
        {
            if (Decimal.TryParse(reader.Value.ToString(), out var result))
            {
                return result;
            }
            return default(Decimal);
        }
        return Convert.ToDecimal(reader.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, decimal value, JsonSerializer serializer)
    {
        if (value == 0)
        {
            writer.WriteValue(0);
        }
        else
        {
            string input = value.ToString().TrimEnd('0').TrimEnd('.');
            writer.WriteValue(Convert.ToDecimal(input));
        }
    }
}