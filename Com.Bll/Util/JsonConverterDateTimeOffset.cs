using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Com.Bll.Util;

/// <summary>
/// 
/// </summary>
public class JsonConverterDateTimeOffset : JsonConverter<DateTimeOffset>
{
    private IHttpContextAccessor? httpContextAccessor;

    public JsonConverterDateTimeOffset(IHttpContextAccessor? httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return existingValue;
    }

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