using System.Reflection;
using Com.Api;
using Com.Api.Admin;
using Com.Db;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NLog.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options =>
{
    options.AddPolicy("any", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});
builder.Services.AddDbContextPool<DbContextEF>(options =>
{
    options.UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddConsole(); }));
    options.EnableSensitiveDataLogging();
    DbContextOptions options1 = options.UseSqlServer(builder.Configuration.GetConnectionString("Mssql")).Options;
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "redisInstance";
});
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("cache_0", new CacheProfile() { Duration = 1 });
    options.CacheProfiles.Add("cache_1", new CacheProfile() { Duration = 5 });
    options.CacheProfiles.Add("cache_2", new CacheProfile() { Duration = 10 });
    options.CacheProfiles.Add("cache_3", new CacheProfile() { Duration = 60 });
}).AddNewtonsoftJson((setupAction) =>
{
    // setupAction.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
    // setupAction.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
    // setupAction.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    // setupAction.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    // setupAction.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    // setupAction.SerializerSettings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects;
    setupAction.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
    setupAction.SerializerSettings.Converters.Add(new Com.Api.JsonConverterDecimal());
#pragma warning disable 
    ServiceProvider build = builder.Services.BuildServiceProvider();
#pragma warning restore
    IHttpContextAccessor? httpContextAccessor = build.GetService<IHttpContextAccessor>();
    if (httpContextAccessor != null)
    {
        setupAction.SerializerSettings.Converters.Add(new JsonConverterDateTimeOffset(httpContextAccessor));
    }
});
builder.Services.AddHostedService<MainService>();
builder.Services.AddResponseCompression();
// builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    var basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
    options.IncludeXmlComments(Path.Combine(basePath!, "Com.Db.xml"));
    options.IncludeXmlComments(Path.Combine(basePath!, "Com.Api.Admin.xml"));
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT授权(数据将在请求头中进行传输) 参数结构: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "bearer",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {{
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }, new List<string>()
    }});
});
builder.Services.AddHostedService<MainService>();
builder.Host.ConfigureLogging((context, logging) =>
{
    logging.ClearProviders();
#if (DEBUG)
    logging.AddConsole();
#endif
});
builder.Host.UseNLog();
var app = builder.Build();
// if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("any");
app.UseResponseCaching();
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCompression();
app.MapControllers();
app.Run();
