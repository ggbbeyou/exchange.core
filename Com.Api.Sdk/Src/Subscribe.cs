using System;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WebSocketSharp;
using System.Timers;
using Newtonsoft.Json;
using Com.Api.Sdk.Models;

namespace Com.Api.Sdk;

/// <summary>
/// websocket接口实现
/// </summary>
public class Subscribe
{
    /// <summary>
    /// 接口主机地址
    /// </summary>
    public readonly string api_host = "ws://localhost:5210/WebSocket/WebSocketUI";
    /// <summary>
    /// 日志接口
    /// </summary>
    private readonly ILogger<Subscribe> logger = null!;
    /// <summary>
    /// 判断超时定时器
    /// </summary>
    private System.Timers.Timer _timer;
    /// <summary>
    /// websocket对象
    /// </summary>
    protected WebSocket _WebSocket = null!;
    /// <summary>
    /// 最后反馈时间
    /// </summary>
    private DateTime _lastReceivedTime;
    /// <summary>
    /// 是否自动重连
    /// </summary>
    public bool _autoConnect;
    /// <summary>
    /// 重新连接等待时长
    /// </summary>
    private const int RECONNECT_WAIT_SECOND = 60;
    /// <summary>
    /// 第二次重新连接等待时长
    /// </summary>
    private const int RENEW_WAIT_SECOND = 120;
    /// <summary>
    /// 定时器间隔时长秒
    /// </summary>
    private const int TIMER_INTERVAL_SECOND = 5;
    /// <summary>
    /// 接收消息回调事件
    /// </summary>
    public event Action<string>? _eventMessage;
    /// <summary>
    /// websocket连接成功回调事件
    /// </summary>
    public event Action<EventArgs>? _eventOpen;

    /// <summary>
    /// 订阅初始化
    /// </summary>
    /// <param name="api_host">IDCM订阅地址</param>
    /// <param name="api_key">key</param>
    /// <param name="contractId">合约id</param>
    /// <param name="user_id">用户ID</param>
    /// <param name="logger">日志接口</param>
    public Subscribe(ILogger<Subscribe>? logger, string api_host = "ws://localhost:5210/WebSocket/WebSocketUI")
    {
        this.api_host = api_host;
        this.logger = logger ?? NullLogger<Subscribe>.Instance;
        _timer = new System.Timers.Timer(TIMER_INTERVAL_SECOND * 1000);
        _timer.Elapsed += _timer_Elapsed;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="api_key"></param>
    /// <param name="key_secret"></param>
    /// <returns></returns>
    public string login(string api_key, string key_secret)
    {
        string SignStr = $"apikey={api_key}&secret_key={key_secret}";
        MD5 md5 = MD5.Create();
        byte[] byteOld = Encoding.UTF8.GetBytes(SignStr);
        byte[] byteNew = md5.ComputeHash(byteOld);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in byteNew)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString().ToUpper();
    }

    /// <summary>
    /// 超时定时器
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            double elapsedSecond = (DateTime.UtcNow - _lastReceivedTime).TotalSeconds;
            if (elapsedSecond > RECONNECT_WAIT_SECOND && elapsedSecond <= RENEW_WAIT_SECOND)
            {
                this.logger.LogTrace("WebSocket reconnecting...");
                _WebSocket.Close();
                Task.Delay(100);
                _WebSocket.Connect();
            }
            else if (elapsedSecond > RENEW_WAIT_SECOND)
            {
                this.logger.LogTrace("WebSocket re-initialize...");
                Disconnect();
                UninitializeWebSocket();
                InitializeWebSocket();
                Connect();
            }
        }
        catch (System.Exception ex)
        {
            this.logger.LogError(ex, "idcm SDK websocket超时重连报错");
        }
    }


    /// <summary>
    /// 初始化websocket
    /// </summary>
    private void InitializeWebSocket()
    {
        _WebSocket = new WebSocket(this.api_host);
        //_WebSocket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.None;
        _WebSocket.OnError += _WebSocket_OnError;
        _WebSocket.OnOpen += _WebSocket_OnOpen;
        _lastReceivedTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 取消websocket连接
    /// </summary>
    private void UninitializeWebSocket()
    {
        _WebSocket.OnOpen -= _WebSocket_OnOpen;
        _WebSocket.OnError -= _WebSocket_OnError;
        // _WebSocket = null;
    }

    /// <summary>
    /// 连接到Websocket服务器
    /// </summary>
    /// <param name="autoConnect">断开连接后是否自动连接到服务器</param>
    public void Connect(bool autoConnect = true)
    {
        _WebSocket.OnMessage += _WebSocket_OnMessage;
        _WebSocket.Connect();
        _autoConnect = autoConnect;
        if (_autoConnect)
        {
            _timer.Enabled = true;
        }
    }

    /// <summary>
    /// 断开与Websocket服务器的连接
    /// </summary>
    public void Disconnect()
    {
        _timer.Enabled = false;
        _WebSocket.OnMessage -= _WebSocket_OnMessage;
        _WebSocket.Close(CloseStatusCode.Normal);
    }

    /// <summary>
    /// websocket连接成功回调函数
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _WebSocket_OnOpen(object? sender, EventArgs e)
    {
        this.logger.LogDebug("WebSocket opened");
        _lastReceivedTime = DateTime.UtcNow;
        if (this._eventOpen != null)
        {
            this._eventOpen(e);
        }
    }

    /// <summary>
    /// websocket 接收到消息回调函数
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _WebSocket_OnMessage(object? sender, MessageEventArgs e)
    {
        _lastReceivedTime = DateTime.UtcNow;
        string data = e.Data;
        if (e.IsBinary)
        {
            // data = GZipDecompresser.Decompress(e.RawData);
        }
        if (this._eventMessage != null)
        {
            this._eventMessage(data);
        }
    }

    /// <summary>
    /// websocket 出错回调函数
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _WebSocket_OnError(object? sender, WebSocketSharp.ErrorEventArgs e)
    {
        this.logger.LogError($"WebSocket error: {e.Message}");
    }

    // /// <summary>
    // /// 发送
    // /// </summary>
    // /// <param name = "obj" ></ param >
    // public void Send<K>(K obj) where K : ReqWebsocker<ReqChannel>
    // {
    //     if (_WebSocket.ReadyState == WebSocketState.Open)
    //     {
    //         _WebSocket.Send(JsonConvert.SerializeObject(obj));
    //     }
    // }

}