using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Com.Bll.Util;
using Com.Db;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Com.Admin
{
    /// <summary>
    /// 启动
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// 配置文件接口
        /// </summary>
        /// <value></value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// 启动
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// 运行时将调用此方法。 使用此方法将服务添加到容器。添加中间件
        /// </summary>
        /// <param name="services">服务集合接口</param>
        public void ConfigureServices(IServiceCollection services)
        {
            NLog.GlobalDiagnosticsContext.Set("NlogDbConStr", Configuration.GetConnectionString("Mssql"));
            services.AddCors(options =>
            {
                options.AddPolicy("any", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
            services.AddResponseCompression();
            services.AddControllers(options =>
            {
                options.CacheProfiles.Add("cache_1", new CacheProfile() { Duration = 5 });
                options.CacheProfiles.Add("cache_2", new CacheProfile() { Duration = 10 });
                options.CacheProfiles.Add("cache_3", new CacheProfile() { Duration = 60 });
            });
            services.AddHostedService<MainService>();
            services.AddDbContext<DbContextEF>(dbContextOptions => dbContextOptions.UseSqlServer(this.Configuration.GetConnectionString("Mssql")));
            //添加jwt验证：
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
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
                            ClaimsIdentity identity = context.Principal.Identities.FirstOrDefault();
                            Claim login_playInfo_id = identity.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
                            try
                            {
                                string redisConnection = this.Configuration.GetConnectionString("redis");
                                ConnectionMultiplexer redisMultiplexer = ConnectionMultiplexer.Connect(redisConnection);
                                IDatabase rdb = redisMultiplexer.GetDatabase();
                                // string timeout_str = rdb.HashGet(Const.redis_blacklist, login_playInfo_id.Value);
                                // if (!string.IsNullOrWhiteSpace(timeout_str))
                                // {
                                // DateTimeOffset timeout = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(timeout_str));
                                // if (timeout.UtcDateTime < DateTime.UtcNow)
                                // {
                                // rdb.HashDelete(Const.redis_blacklist, login_playInfo_id.Value);
                                // }
                                // else
                                // {
                                //     context.Response.StatusCode = 401;
                                //     context.NoResult();
                                // }
                                // }
                            }
                            catch
                            {
                                // logger.LogError(ex, $"redis服务器连接不上,地址:{redisConnection}");
                            }
                        }
                    },
                    //在将质询发送回调用者之前调用。
                    // OnChallenge = context =>
                    // {
                    //     if (context.Response.StatusCode != 200)
                    //     {
                    //         context.HandleResponse();
                    //         context.Response.ContentType = "application/json";
                    //         ModelResult result = new ModelResult();
                    //         result.code = (E_ResultCode)context.Response.StatusCode;
                    //         context.Response.StatusCode = 200;
                    //         context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                    //     }
                    //     return Task.CompletedTask;
                    // },
                    //仿问禁止的地址
                    OnForbidden = context =>
                    {
                        return Task.CompletedTask;
                    },

                };
            });
        }

        /// <summary>
        /// 此方法由运行时调用。 使用此方法配置 HTTP 请求管道。
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment() || env.IsStaging())
            {
                app.UseDeveloperExceptionPage();
            }
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
            //会话
            // app.UseSession();
            // app.Use(async (context, next) =>
            // {
            // await next.Invoke();
            // if (context.Response.StatusCode == 404)
            // {
            //     ModelResult result = new ModelResult();
            //     result.code = E_ResultCode.not_found_url;
            //     string json = JsonConvert.SerializeObject(result);
            //     context.Response.StatusCode = 200;
            //     context.Response.ContentType = "application/json";
            //     await context.Response.Body.WriteAsync(System.Text.Encoding.Default.GetBytes(json));
            // }
            // });
            //路由规则
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Account}/{action=Login}/{id?}");
            });
        }

    }
}