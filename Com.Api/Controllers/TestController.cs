using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Api.Models;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Com.Api.Sdk.Models;
using Com.Bll.Util;

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
        List<Coin> coins = new List<Coin>();
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
        coins.Add(usdt);
        coins.Add(btc);
        coins.Add(eth);

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
        for (int i = 0; i < 10; i++)
        {
            (string public_key, string private_key) key = Encryption.GetRsaKey();
            Users user = new Users()
            {
                user_id = FactoryService.instance.constant.worker.NextId(),
                user_name = "user" + i,
                password = Encryption.SHA256Encrypt("123456_" + i),
                disabled = false,
                transaction = true,
                withdrawal = false,
                phone = null,
                email = null,
                vip = vip1.id,
                public_key = key.public_key,
                private_key = key.private_key,
            };
            this.db.Users.Add(user);
            Wallet wallet_usdt = new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.main,
                user_id = user.user_id,
                user_name = user.user_name,
                coin_id = usdt.coin_id,
                coin_name = usdt.coin_name,
                total = 5_000_000_000,
                available = 5_000_000_000,
                freeze = 0,
            };
            Wallet wallet_btc = new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.main,
                user_id = user.user_id,
                user_name = user.user_name,
                coin_id = btc.coin_id,
                coin_name = btc.coin_name,
                total = 10_000_000,
                available = 10_000_000,
                freeze = 0,
            };
            Wallet wallet_eth = new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.main,
                user_id = user.user_id,
                user_name = user.user_name,
                coin_id = eth.coin_id,
                coin_name = eth.coin_name,
                total = 50_000_000,
                available = 50_000_000,
                freeze = 0,
            };
            this.db.Wallet.Add(wallet_usdt);
            this.db.Wallet.Add(wallet_btc);
            this.db.Wallet.Add(wallet_eth);
        }
        (string public_key, string private_key) key_btc_user = Encryption.GetRsaKey();
        Users settlement_btc_usdt = new Users()
        {
            user_id = FactoryService.instance.constant.worker.NextId(),
            user_name = "settlement_btc/usdt",
            password = Encryption.SHA256Encrypt("123456"),
            disabled = false,
            transaction = true,
            withdrawal = false,
            phone = null,
            email = null,
            vip = vip1.id,
            public_key = key_btc_user.public_key,
            private_key = key_btc_user.private_key,
        };
        this.db.Users.Add(settlement_btc_usdt);
        (string public_key, string private_key) key_eth_user = Encryption.GetRsaKey();
        Users settlement_eth_usdt = new Users()
        {
            user_id = FactoryService.instance.constant.worker.NextId(),
            user_name = "settlement_eth/usdt",
            password = Encryption.SHA256Encrypt("123456"),
            disabled = false,
            transaction = true,
            withdrawal = false,
            phone = null,
            email = null,
            vip = vip1.id,
            public_key = key_eth_user.public_key,
            private_key = key_eth_user.private_key,
        };
        this.db.Users.Add(settlement_eth_usdt);
        foreach (var item in coins)
        {
            this.db.Wallet.Add(new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.fee,
                user_id = settlement_btc_usdt.user_id,
                user_name = settlement_btc_usdt.user_name,
                coin_id = item.coin_id,
                coin_name = item.coin_name,
                total = 0,
                available = 0,
                freeze = 0,
            });
            this.db.Wallet.Add(new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.fee,
                user_id = settlement_eth_usdt.user_id,
                user_name = settlement_eth_usdt.user_name,
                coin_id = item.coin_id,
                coin_name = item.coin_name,
                total = 0,
                available = 0,
                freeze = 0,
            });
            this.db.Wallet.Add(new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.recharge,
                user_id = settlement_btc_usdt.user_id,
                user_name = settlement_btc_usdt.user_name,
                coin_id = item.coin_id,
                coin_name = item.coin_name,
                total = 100_000_000_000,
                available = 100_000_000_000,
                freeze = 0,
            });
            this.db.Wallet.Add(new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.recharge,
                user_id = settlement_eth_usdt.user_id,
                user_name = settlement_eth_usdt.user_name,
                coin_id = item.coin_id,
                coin_name = item.coin_name,
                total = 100_000_000_000,
                available = 100_000_000_000,
                freeze = 0,
            });
            this.db.Wallet.Add(new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.withdraw,
                user_id = settlement_btc_usdt.user_id,
                user_name = settlement_btc_usdt.user_name,
                coin_id = item.coin_id,
                coin_name = item.coin_name,
                total = 0,
                available = 0,
                freeze = 0,
            });
            this.db.Wallet.Add(new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.withdraw,
                user_id = settlement_eth_usdt.user_id,
                user_name = settlement_eth_usdt.user_name,
                coin_id = item.coin_id,
                coin_name = item.coin_name,
                total = 0,
                available = 0,
                freeze = 0,
            });
        }
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
            amount_multiple = 0.00001m,
            fee_market_buy = 0.00003m,
            fee_market_sell = 0.00003m,
            fee_limit_buy = 0.001m,
            fee_limit_sell = 0.003m,
            market_uid = 0,
            settlement_uid = settlement_btc_usdt.user_id,
            last_price = Math.Round((decimal)FactoryService.instance.constant.random.NextDouble(), 2),
            service_url = "http://localhost:8080",
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
            amount_multiple = 0.0002m,
            fee_market_buy = 0.00003m,
            fee_market_sell = 0.00003m,
            fee_limit_buy = 0.001m,
            fee_limit_sell = 0.003m,
            market_uid = 0,
            settlement_uid = settlement_eth_usdt.user_id,
            last_price = Math.Round((decimal)FactoryService.instance.constant.random.NextDouble(), 2),
            service_url = "http://localhost:8080",
        };
        this.db.Market.Add(btcusdt);
        this.db.Market.Add(ethusdt);
        return Json(this.db.SaveChanges());
    }


    /// <summary>
    /// 模拟下单
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public IActionResult PlaceOrderText()
    {
        List<Users> users = this.db.Users.ToList();
        List<Market> markets = this.db.Market.ToList();
        for (int i = 0; i < 10; i++)
        {
            Users user = users[FactoryService.instance.constant.random.Next(0, 10)];
            Market market = markets[FactoryService.instance.constant.random.Next(0, 2)];
            List<ReqOrder> reqOrders = new List<ReqOrder>();
            for (int j = 0; j < 500; j++)
            {
                E_OrderSide side = FactoryService.instance.constant.random.Next(0, 2) == 0 ? E_OrderSide.buy : E_OrderSide.sell;
                E_OrderType type = FactoryService.instance.constant.random.Next(0, 2) == 0 ? E_OrderType.price_limit : E_OrderType.price_market;
                decimal amount = (decimal)FactoryService.instance.constant.random.NextDouble();
                decimal? price = (decimal)FactoryService.instance.constant.random.NextDouble();
                price = Math.Round(price ?? 0, market.price_places);
                if (amount == 0 || price == 0)
                {
                    continue;
                }
                if (type == E_OrderType.price_market)
                {
                    price = null;
                    if (side == E_OrderSide.buy)
                    {
                        amount = Math.Round(amount, market.price_places);
                    }
                }
                else if (type == E_OrderType.price_limit)
                {
                    amount = Math.Round(amount / market.amount_multiple, 0, MidpointRounding.ToNegativeInfinity) * market.amount_multiple;
                }
                if (side == E_OrderSide.sell)
                {
                    amount = Math.Round(amount / market.amount_multiple, 0, MidpointRounding.ToNegativeInfinity) * market.amount_multiple;
                }
                ReqOrder order = new ReqOrder()
                {
                    client_id = FactoryService.instance.constant.worker.NextId().ToString(),
                    symbol = market.symbol,
                    side = side,
                    type = type,
                    price = price,
                    amount = amount,
                    trigger_hanging_price = 0,
                    trigger_cancel_price = 0,
                    data = null,
                };

                reqOrders.Add(order);
            }
            order_service.PlaceOrder(market.symbol, user.user_id, user.user_name, reqOrders);
        }
        return Json("");
    }

}
