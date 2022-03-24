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

    public IActionResult Init()
    {
        Coin btc = new Coin()
        {

        };

        return View();
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
