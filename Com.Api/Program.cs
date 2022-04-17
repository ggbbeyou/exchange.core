using System.Reflection;
using System.Security.Claims;
using System.Text;
using Com.Api;
using Com.Api.Sdk.Enum;
using Com.Bll;
using Com.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog.Web;
using StackExchange.Redis;

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
    options.EnableSensitiveDataLogging();
    DbContextOptions options1 = options.UseSqlServer(builder.Configuration.GetConnectionString("Mssql")).Options;
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "redisInstance";
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
        OnTokenValidated = context =>
        {
            if (context != null && context.Principal != null && context.Principal.Claims != null)
            {
                ClaimsIdentity? identity = context.Principal.Identities.FirstOrDefault();
                if (identity != null)
                {
                    long no = 0, user_id = 0;
                    E_App app = E_App.undefined;
                    Claim? claim = identity.Claims.FirstOrDefault(P => P.Type == "no");
                    if (claim != null)
                    {
                        no = long.Parse(claim.Value);
                    }
                    claim = identity.Claims.FirstOrDefault(P => P.Type == "user_id");
                    if (claim != null)
                    {
                        user_id = long.Parse(claim.Value);
                    }
                    claim = identity.Claims.FirstOrDefault(P => P.Type == "app");
                    if (claim != null)
                    {
                        app = (E_App)Enum.Parse(typeof(E_App), claim.Value);
                    }
                    RedisValue rv = FactoryService.instance.constant.redis.HashGet(FactoryService.instance.GetRedisBlacklist(), $"{user_id}_{app}_{no}");
                    if (rv.HasValue)
                    {
                        context.Fail("无效的用户");
                    }
                }
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddResponseCompression();
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("cache_0", new CacheProfile() { Duration = 1 });
    options.CacheProfiles.Add("cache_1", new CacheProfile() { Duration = 5 });
    options.CacheProfiles.Add("cache_2", new CacheProfile() { Duration = 10 });
    options.CacheProfiles.Add("cache_3", new CacheProfile() { Duration = 60 });
}).AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    // options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
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
    options.OperationFilter<MyHeaderFilter>();
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
