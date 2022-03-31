using System.Reflection;
using System.Security.Claims;
using System.Text;
using Com.Api;
using Com.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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
    // options.UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddConsole(); }));
    // options.EnableSensitiveDataLogging();
    DbContextOptions options1 = options.UseSqlServer(builder.Configuration.GetConnectionString("Mssql")).Options;
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "redisInstance";
});
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("cache_1", new CacheProfile() { Duration = 5 });
    options.CacheProfiles.Add("cache_2", new CacheProfile() { Duration = 10 });
    options.CacheProfiles.Add("cache_3", new CacheProfile() { Duration = 60 });
});
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,//是否在令牌期间验证签发者
        ValidateAudience = true,//是否验证接收者
        ValidateLifetime = true,//是否验证失效时间
        ValidateIssuerSigningKey = true,//是否验证签名
        ValidIssuer = builder.Configuration["Jwt:Issuer"],//签发者，签发的Token的人
        ValidAudience = builder.Configuration["Jwt:Audience"],//接收者
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
    };
    options.Events = new JwtBearerEvents()
    {
        //验证失败
        OnAuthenticationFailed = context =>
        {
            return Task.CompletedTask;
        },
        //仿问禁止的地址
        OnForbidden = context =>
        {
            return Task.CompletedTask;
        },
        //仿问地址
        OnMessageReceived = context =>
        {
            return Task.CompletedTask;
        },
        //进行验证
        OnTokenValidated = async context =>
        {
            if (context != null && context.Principal != null && context.Principal.Claims != null)
            {
                ClaimsIdentity identity = context.Principal.Identities.FirstOrDefault();
                // Claim login_playInfo_id = identity.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
                // Claim login_playInfo_no = identity.Claims.FirstOrDefault(P => P.Type == "http://schemas.microsoft.com/claims/authnmethodsreferences");
                // Claim app = identity.Claims.FirstOrDefault(P => P.Type == "app");
                // var redisClient = context.HttpContext.RequestServices.GetRequiredService<IRedisCacheClient>();
                // string timeout_str = await redisClient.GetDbFromConfiguration().HashGetAsync<string>(Const.redis_blacklist, $"{login_playInfo_id.Value}_{login_playInfo_no.Value}");
                //             // if (!string.IsNullOrWhiteSpace(timeout_str) && app.Value != "proxy")
                //             if (!string.IsNullOrWhiteSpace(timeout_str))
                // {
                //     string[] values = timeout_str.Split('_');
                //     DateTimeOffset timeout = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(values[0]));
                //     if (timeout.DateTime < DateTime.UtcNow)
                //     {
                //         await redisClient.GetDbFromConfiguration().HashDeleteAsync(Const.redis_blacklist, $"{login_playInfo_id.Value}_{login_playInfo_no.Value}");
                //     }
                //     if (values.Length > 1)
                //     {
                //         context.Response.StatusCode = Convert.ToInt32(values[1]);
                //     }
                //     else
                //     {
                //         context.Response.StatusCode = 9006;
                //     }
                //     context.NoResult();
                // }
            }
        },
        //仿问没权限的
        OnChallenge = context =>
        {
            if (context.Response.StatusCode == 200)
            {
                // context.HandleResponse();
                // context.Response.ContentType = "application/json";
                // ModelResult result = new ModelResult();
                // result.code = ResultCode.no_permission;
                // context.Response.StatusCode = 200;
                // context.Response.WriteAsync(JsonConvert.SerializeObject(result));
            }
            // else if (Enum.IsDefined(typeof(ResultCode), context.Response.StatusCode))
            {
                // context.HandleResponse();
                // context.Response.ContentType = "application/json";
                // ModelResult result = new ModelResult();
                // result.code = (ResultCode)context.Response.StatusCode;
                // context.Response.StatusCode = 200;
                // context.Response.WriteAsync(JsonConvert.SerializeObject(result));
            }
            return Task.CompletedTask;
        },
    };
});
builder.Services.AddResponseCompression();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
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
var app = builder.Build();
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
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
