using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;
using Com.Db.Model;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Com.Api.Sdk.Models;

namespace Com.Api.Controllers;

// [Route("api/[controller]/[action]")]
// [Authorize]
[AllowAnonymous]
public class TestController : Controller
{
    public DbContextEF db = null!;

    /// <summary>
    /// 登录玩家id
    /// </summary>
    /// <value></value>
    public int user_id
    {
        get
        {
            Claim? claim = User.Claims.FirstOrDefault(P => P.Type == JwtRegisteredClaimNames.Aud);
            if (claim != null)
            {
                return Convert.ToInt32(claim.Value);
            }
            return 5;
        }
    }

    /// <summary>
    /// 交易对基础信息
    /// </summary>
    /// <returns></returns>
    public ServiceMarket market_info_db = new ServiceMarket();
    /// <summary>
    /// Service:订单
    /// </summary>
    public ServiceOrder order_service = new ServiceOrder();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="environment"></param>
    /// <param name="provider"></param>
    /// <param name="logger"></param>
    public TestController(IServiceProvider provider, IConfiguration configuration, IHostEnvironment environment, DbContextEF db, ILogger<OrderController> logger)
    {
        this.db = db;
    }

    /// <summary>
    /// 初始化配置
    /// </summary>
    /// <returns></returns>
    public IActionResult Init()
    {
        Coin usdt = new Coin()
        {
            coin_id = FactoryService.instance.constant.worker.NextId(),
            coin_name = "usdt",
            price_places = 8,
            amount_places = 8,
            contract = "",
        };
        Coin btc = new Coin()
        {
            coin_id = FactoryService.instance.constant.worker.NextId(),
            coin_name = "btc",
            price_places = 8,
            amount_places = 8,
            contract = "",
        };
        Coin eth = new Coin()
        {
            coin_id = FactoryService.instance.constant.worker.NextId(),
            coin_name = "eth",
            price_places = 8,
            amount_places = 8,
            contract = "",
        };
        this.db.Coin.Add(usdt);
        this.db.Coin.Add(btc);
        this.db.Coin.Add(eth);
        Market btcusdt = new Market()
        {
            market = FactoryService.instance.constant.worker.NextId(),
            symbol = "btc/usdt",
            coin_id_base = btc.coin_id,
            coin_name_base = btc.coin_name,
            coin_id_quote = usdt.coin_id,
            coin_name_quote = usdt.coin_name,
            separator = "/",
            price_places = 2,
            amount_places = 5,
            amount_multiple = 0,
            fee_market_buy = 0.00003m,
            fee_market_sell = 0.00003m,
            fee_limit_buy = 0.001m,
            fee_limit_sell = 0.003m,
            market_uid = 0,
        };
        Market ethusdt = new Market()
        {
            market = FactoryService.instance.constant.worker.NextId(),
            symbol = "eth/usdt",
            coin_id_base = eth.coin_id,
            coin_name_base = eth.coin_name,
            coin_id_quote = usdt.coin_id,
            coin_name_quote = usdt.coin_name,
            separator = "/",
            price_places = 2,
            amount_places = 4,
            amount_multiple = 0,
            fee_market_buy = 0.00003m,
            fee_market_sell = 0.00003m,
            fee_limit_buy = 0.001m,
            fee_limit_sell = 0.003m,
            market_uid = 0,
        };
        this.db.Market.Add(btcusdt);
        this.db.Market.Add(ethusdt);
        Vip vip1 = new Vip()
        {
            id = FactoryService.instance.constant.worker.NextId(),
            name = "vip1",
            fee_market = 0.001m,
            fee_limit = 0.001m,
        };
        Vip vip2 = new Vip()
        {
            id = FactoryService.instance.constant.worker.NextId(),
            name = "vip2",
            fee_market = 0.002m,
            fee_limit = 0.002m,
        };
        

        this.db.Vip.Add(vip1);
        this.db.Vip.Add(vip2);
        return Json(this.db.SaveChanges());
    }


    [HttpPost]
    public IActionResult PlaceOrderText()
    {
        // List<PlaceOrder> orders = new List<PlaceOrder>();
        // Random r = new Random();
        // for (int i = 0; i < 500; i++)
        // {
        //     PlaceOrder orderResult = new PlaceOrder();
        //     orderResult.side = r.Next(0, 2) == 0 ? E_OrderSide.buy : E_OrderSide.sell;
        //     orderResult.type = r.Next(0, 2) == 0 ? E_OrderType.price_limit : E_OrderType.price_market;
        //     orderResult.client_id = null;
        //     if (orderResult.type == E_OrderType.price_limit)
        //     {
        //         orderResult.price = (decimal)FactoryService.instance.constant.random.Next(1, 30);
        //     }
        //     else
        //     {
        //         orderResult.price = null;
        //     }
        //     orderResult.amount = (decimal)FactoryService.instance.constant.random.Next(1, 30);
        //     orders.Add(orderResult);
        // }
        // PlaceOrder("eth/usdt", orders);
        // List<PlaceOrder> orders1 = new List<PlaceOrder>();
        // for (int i = 0; i < 500; i++)
        // {
        //     PlaceOrder orderResult = new PlaceOrder();
        //     orderResult.side = r.Next(0, 2) == 0 ? E_OrderSide.buy : E_OrderSide.sell;
        //     orderResult.type = r.Next(0, 2) == 0 ? E_OrderType.price_limit : E_OrderType.price_market;
        //     orderResult.client_id = null;
        //     if (orderResult.type == E_OrderType.price_limit)
        //     {
        //         orderResult.price = (decimal)FactoryService.instance.constant.random.Next(1, 30);
        //     }
        //     else
        //     {
        //         orderResult.price = null;
        //     }
        //     orderResult.amount = (decimal)FactoryService.instance.constant.random.Next(1, 30);
        //     orders1.Add(orderResult);
        // }
        // PlaceOrder("btc/usdt", orders);
        return Json(new { });
    }

}
