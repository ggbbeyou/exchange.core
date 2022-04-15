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

}