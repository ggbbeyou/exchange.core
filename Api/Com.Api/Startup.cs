using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Com.Db;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
// using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace Com.Api;

/// <summary>
/// 启动类
/// </summary>
public class Startup
{
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="configuration"></param>
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// 配置文件接口
    /// </summary>
    /// <value></value>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// 运行时将调用此方法。 使用此方法将服务添加到容器。添加中间件
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("any", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            });
        });
        services.AddDbContextPool<DbContextEF>(options =>
        {
            options.UseLoggerFactory(LoggerFactory.Create(builder => { builder.AddConsole(); }));
            options.EnableSensitiveDataLogging();
            DbContextOptions options1 = options.UseSqlServer(Configuration.GetConnectionString("Mssql")).Options;
        });
        services.AddResponseCompression();
        services.AddDistributedMemoryCache();
        services.AddControllers(options =>
        {
            options.CacheProfiles.Add("cache_1", new CacheProfile() { Duration = 5 });
            options.CacheProfiles.Add("cache_2", new CacheProfile() { Duration = 10 });
            options.CacheProfiles.Add("cache_3", new CacheProfile() { Duration = 60 });
        });
        // services.AddSwaggerGen(c =>
        // {
        //     string? basePath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
        //     if (basePath != null)
        //     {
        //         c.IncludeXmlComments(Path.Combine(basePath, "Com.Api.xml"));
        //     }
        //     c.SwaggerDoc("v1", new OpenApiInfo { Title = "Com.Api", Version = "v1" });
        //     c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        //     {
        //         Description = "JWT授权(数据将在请求头中进行传输) 参数结构: \"Authorization: Bearer {token}\"",
        //         Name = "Authorization",
        //         In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        //         Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        //         BearerFormat = "JWT",
        //         Scheme = "bearer",
        //     });
        //     c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        //     {{
        //             new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        //             {
        //                 Reference = new Microsoft.OpenApi.Models.OpenApiReference
        //                 {
        //                     Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
        //                     Id = "Bearer"
        //                 }
        //             }, new List<string>()
        //         }});
        // });
        services.AddControllersWithViews();
        services.AddHostedService<MainService>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
        {
            //登录路径：这是当用户试图访问资源但未经过身份验证时，程序将会将请求重定向到这个相对路径
            o.LoginPath = new PathString("/account/login");
            //禁止访问路径：当用户试图访问资源时，但未通过该资源的任何授权策略，请求将被重定向到这个相对路径。
            o.AccessDeniedPath = new PathString("/home/privacy");
        });
        //添加jwt验证：
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(async options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.FromMinutes(5000),//时钟偏差补偿服务器时间漂移。
                // IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Const.publicKey)),//拿到SecurityKey
                RequireSignedTokens = true,//需要签名令牌
                RequireExpirationTime = true,//确保令牌未过期：
                ValidateLifetime = true,//验证生命周期
                ValidateAudience = true,//确保令牌受众与我们的受众价值相匹配（默认为 true）：
                                        // ValidAudience = Const.domain,//有效受众
                ValidateIssuer = true,//确保令牌由受信任的授权服务器颁发（默认为 true）
                                      // ValidIssuer = Const.domain,//有效发行者
                ValidateIssuerSigningKey = true,//是否验证SecurityKey
            };
            options.Events = new JwtBearerEvents()
            {
                //当第一次收到协议消息时调用。
                OnMessageReceived = context =>
                {
                    return Task.CompletedTask;
                },
                //验证失败
                OnAuthenticationFailed = context =>
                {
                    // context.Response.StatusCode = (int)E_ResultCode.no_permission;
                    return Task.CompletedTask;
                },
                //验证成功
                OnTokenValidated = async context =>
                {
                    var bbb = context.Scheme.Name;
                    if (context != null && context.Principal != null && context.Principal.Claims != null)
                    {
                        // ClaimsIdentity identity = context.Principal.Identities.FirstOrDefault();
                        // Claim login_playInfo_id = identity.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
                        // try
                        // {
                        //     string redisConnection = this.Configuration.GetConnectionString("redis");
                        //     ConnectionMultiplexer redisMultiplexer = ConnectionMultiplexer.Connect(redisConnection);
                        //     IDatabase rdb = redisMultiplexer.GetDatabase();
                        //     string timeout_str = rdb.HashGet(Const.redis_blacklist, login_playInfo_id.Value);
                        //     if (!string.IsNullOrWhiteSpace(timeout_str))
                        //     {
                        //         DateTimeOffset timeout = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(timeout_str));
                        //         if (timeout.UtcDateTime < DateTime.UtcNow)
                        //         {
                        //             rdb.HashDelete(Const.redis_blacklist, login_playInfo_id.Value);
                        //         }
                        //         else
                        //         {
                        //             context.Response.StatusCode = 401;
                        //             context.NoResult();
                        //         }
                        //     }
                        // }
                        // catch
                        // {
                        //     // logger.LogError(ex, $"redis服务器连接不上,地址:{redisConnection}");
                        // }
                    }
                },
                //在将质询发送回调用者之前调用。
                OnChallenge = context =>
                {
                    if (context.Response.StatusCode != 200)
                    {
                        // context.HandleResponse();
                        // context.Response.ContentType = "application/json";
                        // ModelResult result = new ModelResult();
                        // result.code = (E_ResultCode)context.Response.StatusCode;
                        // context.Response.StatusCode = 200;
                        // context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                    }
                    return Task.CompletedTask;
                },
                //仿问禁止的地址
                OnForbidden = context =>
                {
                    return Task.CompletedTask;
                },
            };
        });
    }

    /// <summary>
    /// 运行时将调用此方法。 使用此方法来配置HTTP请求管道。
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || env.IsStaging())
        {
            app.UseDeveloperExceptionPage();
            // app.UseSwagger();
            // app.UseSwaggerUI(c =>
            // {
            //     c.SwaggerEndpoint("/swagger/v1/swagger.json", "Com.Api v1");
            //     c.RoutePrefix = string.Empty;
            // });
        }
        app.UseWebSockets();
        //路由
        app.UseRouting();
        //跨域           
        app.UseCors("any");
        //响应缓存
        app.UseResponseCaching();
        //添加jwt验证 认证方式
        app.UseAuthentication();
        //授权
        app.UseAuthorization();
        //响应压缩
        app.UseResponseCompression();
        app.Use(async (context, next) =>
        {
            await next.Invoke();
            if (context.Response.StatusCode == 404)
            {
                // ModelResult result = new ModelResult();
                // result.code = E_ResultCode.not_found_url;
                // string json = JsonConvert.SerializeObject(result);
                // context.Response.StatusCode = 200;
                // context.Response.ContentType = "application/json";
                // await context.Response.Body.WriteAsync(System.Text.Encoding.Default.GetBytes(json));
            }
        });
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=Login}/{id?}");
        });
    }
}

