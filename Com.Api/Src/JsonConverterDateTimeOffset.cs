// using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Com.Api;

/// <summary>
/// 
/// </summary>
public class JsonConverterDateTimeOffset : JsonConverter<DateTimeOffset>
{
    private IHttpContextAccessor? httpContextAccessor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public JsonConverterDateTimeOffset(IHttpContextAccessor? httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="hasExistingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return existingValue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
    {
        if (httpContextAccessor == null)
        {
            writer.WriteValue(value);
        }
        else
        {
            if (httpContextAccessor.HttpContext == null)
            {
                writer.WriteValue(value);
            }
            else
            {
                int? time_zone = httpContextAccessor.HttpContext.Session.GetInt32("time_zone");
                if (time_zone == null)
                {
                    writer.WriteValue(value);
                }
                else
                {
                    writer.WriteValue(value.ToOffset(new TimeSpan(time_zone.Value, 0, 0)));
                }
            }
        }
    }
}