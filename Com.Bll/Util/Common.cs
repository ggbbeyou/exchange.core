using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using SixLabors;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Com.Bll.Util;

/// <summary>
/// 通用类
/// </summary>
public class Common
{
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger logger;

    /// <summary>
    /// 初始化
    /// </summary>
    public Common()
    {
        this.logger = NullLogger.Instance;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="logger">日志接口</param>
    public Common(ILogger logger)
    {
        this.logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// 压缩字符
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    public byte[] Compression(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        using (var compressedStream = new MemoryStream())
        using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
        {
            zipStream.Write(bytes, 0, bytes.Length);
            zipStream.Close();
            bytes = compressedStream.ToArray();
            return bytes;
        }
    }

    /// <summary>
    /// 生成验证码
    /// </summary>
    /// <param name="n">位数</param>
    /// <returns>验证码字符串</returns>
    public string CreateRandomCode(int n)
    {
        //产生验证码的字符集(去除I 1 l L，O 0等易混字符)
        string charSet = "2,3,4,5,6,8,9,A,B,C,D,E,F,G,H,J,K,M,N,P,R,S,U,W,X,Y";
        string[] CharArray = charSet.Split(',');
        string randomCode = "";
        int temp = -1;
        Random rand = new Random();
        for (int i = 0; i < n; i++)
        {
            if (temp != -1)
            {
                rand = new Random(i * temp * ((int)DateTime.Now.Ticks));
            }
            int t = rand.Next(CharArray.Length - 1);
            if (temp == t)
            {
                return CreateRandomCode(n);
            }
            temp = t;
            randomCode += CharArray[t];
        }
        return randomCode;
    }

    /// <summary>
    /// 生成验证码图片流
    /// </summary>
    /// <param name="code">验证码</param>
    /// <returns></returns>
    public byte[] CreateImage(string code)
    {
        using var ms = new System.IO.MemoryStream();
        try
        {
            SixLabors.ImageSharp.Color[] Colors = { SixLabors.ImageSharp.Color.Black, SixLabors.ImageSharp.Color.Red, SixLabors.ImageSharp.Color.Blue, SixLabors.ImageSharp.Color.Green };
            char[] Chars = { '0' };
            int Width = 90;
            int Height = 35;
            var r = new Random();
            using var image = new Image<Rgba32>(Width, Height);
            // 字体
            var font = SystemFonts.CreateFont(SystemFonts.Families.First().Name, 25, FontStyle.Bold);
            image.Mutate(ctx =>
            {
                // 白底背景
                ctx.Fill(Color.White);
                // 画验证码
                for (int i = 0; i < code.Length; i++)
                {
                    ctx.DrawText(code[i].ToString(), font, Colors[r.Next(Colors.Length)], new PointF(20 * i + 10, r.Next(2, 12)));
                }
                // 画干扰线
                for (int i = 0; i < 10; i++)
                {
                    var pen = new Pen(Colors[r.Next(Colors.Length)], 1);
                    var p1 = new PointF(r.Next(Width), r.Next(Height));
                    var p2 = new PointF(r.Next(Width), r.Next(Height));
                    ctx.DrawLines(pen, p1, p2);
                }
                // 画噪点
                for (int i = 0; i < 80; i++)
                {
                    var pen = new Pen(Colors[r.Next(Colors.Length)], 1);
                    var p1 = new PointF(r.Next(Width), r.Next(Height));
                    var p2 = new PointF(p1.X + 1f, p1.Y + 1f);
                    ctx.DrawLines(pen, p1, p2);
                }
            });
            // gif 格式
            image.SaveAsGif(ms);
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(ex, $"生成图片出错");
        }
        return ms.ToArray();
    }

    // /// <summary>
    // /// 生成token
    // /// </summary>
    // /// <param name="host">发布者</param>
    // /// <param name="public_key">公钥</param>
    // /// <param name="user_id">id</param>
    // /// <param name="no">登录唯一码</param>
    // /// <param name="account">账号</param>
    // /// <param name="name">名称</param>
    // /// <param name="timeout">超时</param>
    // /// <param name="app">终端</param>
    // /// <returns></returns>       
    // public string CreateToken(string host, string public_key, long user_id, string no, string account, string name, DateTime timeout, string app = "html5")
    // {
    //     var claims = new[]
    //                 {
    //                     new Claim(JwtRegisteredClaimNames.Iss,host),
    //                     new Claim(JwtRegisteredClaimNames.Aud,user_id.ToString()),
    //                     new Claim(JwtRegisteredClaimNames.Amr,no),
    //                     new Claim(JwtRegisteredClaimNames.GivenName,account),
    //                     new Claim(JwtRegisteredClaimNames.FamilyName,name),
    //                     new Claim(JwtRegisteredClaimNames.AuthTime,$"{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}"),
    //                     new Claim("app",app),
    //                 };
    //     var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(public_key));
    //     var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    //     var token = new JwtSecurityToken(
    //         issuer: host,
    //         audience: host,
    //         claims: claims,
    //         expires: timeout,
    //         signingCredentials: creds);
    //     return new JwtSecurityTokenHandler().WriteToken(token);
    // }

}