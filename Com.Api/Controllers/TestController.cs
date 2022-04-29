using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Com.Bll;
using Com.Db;
using Com.Api.Sdk.Enum;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Com.Api.Sdk.Models;
using Com.Bll.Util;
using Newtonsoft.Json;

namespace Com.Api.Controllers;

/// <summary>
/// 
/// </summary>
[ApiController]
[Authorize]
[AllowAnonymous]
[Route("[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// 
    /// </summary>
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
    /// <param name="db"></param>
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
    [HttpGet]
    [Route("Init")]
    public int Init()
    {
        List<Coin> coins = new List<Coin>();
        Coin usdt = new Coin()
        {
            coin_id = FactoryService.instance.constant.worker.NextId(),
            coin_name = "USDT",
            full_name = "USDT",
            icon = "https://www.baidu.com/img/bd_logo1.png",
            contract = "",
        };
        Coin btc = new Coin()
        {
            coin_id = FactoryService.instance.constant.worker.NextId(),
            coin_name = "BTC",
            full_name = "BTC",
            icon = "https://www.baidu.com/img/bd_logo1.png",
            contract = "",
        };
        Coin eth = new Coin()
        {
            coin_id = FactoryService.instance.constant.worker.NextId(),
            coin_name = "ETH",
            full_name = "ETH",
            icon = "https://www.baidu.com/img/bd_logo1.png",
            contract = "",
        };
        this.db.Coin.Add(usdt);
        this.db.Coin.Add(btc);
        this.db.Coin.Add(eth);
        coins.Add(usdt);
        coins.Add(btc);
        coins.Add(eth);
        Vip vip0 = new Vip()
        {
            id = FactoryService.instance.constant.worker.NextId(),
            name = "vip0",
            fee_maker = 0.0002m,
            fee_taker = 0.0004m,
        };
        Vip vip1 = new Vip()
        {
            id = FactoryService.instance.constant.worker.NextId(),
            name = "vip1",
            fee_maker = 0.00016m,
            fee_taker = 0.0004m,
        };
        Vip vip2 = new Vip()
        {
            id = FactoryService.instance.constant.worker.NextId(),
            name = "vip2",
            fee_maker = 0.00014m,
            fee_taker = 0.00035m,
        };
        this.db.Vip.Add(vip0);
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
                user_type = E_UserType.general,
                disabled = false,
                transaction = true,
                withdrawal = false,
                phone = null,
                email = "user@" + i,
                vip = vip0.id,
                public_key = key.public_key,
                private_key = key.private_key,
            };
            this.db.Users.Add(user);
            UsersApi api = new UsersApi()
            {
                id = FactoryService.instance.constant.worker.NextId(),
                user_id = user.user_id,
                api_key = Guid.NewGuid().ToString().Replace("-", ""),
                api_secret = Encryption.SHA256Encrypt(Guid.NewGuid().ToString().Replace("-", "") + "_" + user.user_id + Guid.NewGuid().ToString()),
                transaction = true,
                withdrawal = false,
                white_list_ip = "",
                create_time = DateTimeOffset.UtcNow,

            };
            this.db.UsersApi.Add(api);
            Wallet wallet_usdt = new Wallet()
            {
                wallet_id = FactoryService.instance.constant.worker.NextId(),
                wallet_type = E_WalletType.spot,
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
                wallet_type = E_WalletType.spot,
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
                wallet_type = E_WalletType.spot,
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
            user_type = E_UserType.settlement,
            disabled = false,
            transaction = true,
            withdrawal = false,
            phone = null,
            email = "settlement_btc/usdt@126.com",
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
            user_type = E_UserType.settlement,
            disabled = false,
            transaction = true,
            withdrawal = false,
            phone = null,
            email = "settlement_eth/usdt@126.com",
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
                wallet_type = E_WalletType.main,
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
                wallet_type = E_WalletType.main,
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
            symbol = "BTC/USDT",
            coin_id_base = btc.coin_id,
            coin_name_base = btc.coin_name,
            coin_id_quote = usdt.coin_id,
            coin_name_quote = usdt.coin_name,
            market_type = E_MarketType.spot,
            places_price = 2,
            places_amount = 6,
            trade_min = 10,
            trade_min_market_sell = 0.0002m,
            market_uid = 0,
            status = false,
            transaction = true,
            settlement_uid = settlement_btc_usdt.user_id,
            last_price = 50000,
            service_url = "http://localhost:8000",
        };
        Market ethusdt = new Market()
        {
            market = FactoryService.instance.constant.worker.NextId(),
            symbol = "eth/usdt",
            coin_id_base = eth.coin_id,
            coin_name_base = eth.coin_name,
            coin_id_quote = usdt.coin_id,
            coin_name_quote = usdt.coin_name,
            market_type = E_MarketType.spot,
            places_price = 2,
            places_amount = 4,
            trade_min = 10,
            trade_min_market_sell = 0.002m,
            market_uid = 0,
            status = false,
            transaction = true,
            settlement_uid = settlement_eth_usdt.user_id,
            last_price = 5000,
            service_url = "http://localhost:8000",
        };
        this.db.Market.Add(btcusdt);
        this.db.Market.Add(ethusdt);
        return this.db.SaveChanges();
    }

    /// <summary>
    /// 模拟下单
    /// </summary>
    /// <param name="count">次数</param>
    /// <returns></returns>
    [HttpPost]
    [Route("PlaceOrderText")]
    public Res<List<ResOrder>> PlaceOrderText(int count)
    {
        Res<List<ResOrder>> res = new Res<List<ResOrder>>();
        res.success = true;
        res.code = E_Res_Code.ok;
        res.data = new List<ResOrder>();
        List<Users> users = this.db.Users.ToList();
        List<Market> markets = this.db.Market.ToList();
        for (int i = 0; i < count; i++)
        {
            Users user = users[FactoryService.instance.constant.random.Next(0, 10)];
            Market market = markets[FactoryService.instance.constant.random.Next(0, 2)];
            List<ReqOrder> reqOrders = new List<ReqOrder>();
            for (int j = 0; j < 1000; j++)
            {
                E_OrderSide side = FactoryService.instance.constant.random.Next(0, 2) == 0 ? E_OrderSide.buy : E_OrderSide.sell;
                E_OrderType type = FactoryService.instance.constant.random.Next(0, 2) == 0 ? E_OrderType.limit : E_OrderType.market;
                decimal? price = null;
                decimal? amount = null;
                decimal? total = null;
                if (market.symbol == "btc/usdt")
                {
                    if (type == E_OrderType.limit)
                    {
                        price = FactoryService.instance.constant.random.NextInt64(50000, 50000) + (decimal)Math.Round(FactoryService.instance.constant.random.NextDouble(), market.places_price);
                        amount = FactoryService.instance.constant.random.NextInt64(0, 1) + Math.Round((decimal)FactoryService.instance.constant.random.NextDouble() + market.trade_min_market_sell, market.places_amount);
                    }
                    else if (type == E_OrderType.market)
                    {
                        if (side == E_OrderSide.buy)
                        {
                            total = FactoryService.instance.constant.random.NextInt64(30, 50) + Math.Round((decimal)FactoryService.instance.constant.random.NextDouble() + market.trade_min, market.places_price + market.places_amount);
                        }
                        else if (side == E_OrderSide.sell)
                        {
                            amount = (decimal)FactoryService.instance.constant.random.NextDouble();
                            if (amount < market.trade_min_market_sell)
                            {
                                amount += market.trade_min_market_sell;
                            }
                            amount = Math.Round(amount ?? 0, market.places_amount);
                        }
                    }
                }
                else if (market.symbol == "eth/usdt")
                {
                    if (type == E_OrderType.limit)
                    {
                        price = FactoryService.instance.constant.random.NextInt64(5000, 5000) + (decimal)Math.Round(FactoryService.instance.constant.random.NextDouble(), market.places_price);
                        amount = FactoryService.instance.constant.random.NextInt64(0, 2) + Math.Round((decimal)FactoryService.instance.constant.random.NextDouble() + market.trade_min_market_sell, market.places_amount);
                    }
                    else if (type == E_OrderType.market)
                    {
                        if (side == E_OrderSide.buy)
                        {
                            total = FactoryService.instance.constant.random.NextInt64(20, 60) + Math.Round((decimal)FactoryService.instance.constant.random.NextDouble() + market.trade_min, market.places_price + market.places_amount);
                        }
                        else if (side == E_OrderSide.sell)
                        {
                            amount = (decimal)FactoryService.instance.constant.random.NextDouble();
                            if (amount < market.trade_min_market_sell)
                            {
                                amount += market.trade_min_market_sell;
                            }
                            amount = Math.Round(amount ?? 0, market.places_amount);
                        }
                    }
                }
                ReqOrder order = new ReqOrder()
                {
                    client_id = FactoryService.instance.constant.worker.NextId().ToString(),
                    symbol = market.symbol,
                    side = side,
                    type = type,
                    trade_model = E_TradeModel.cash,
                    price = price,
                    amount = amount,
                    total = total,
                    trigger_hanging_price = 0,
                    trigger_cancel_price = 0,
                };
                reqOrders.Add(order);
            }
            Res<List<ResOrder>> aaa = order_service.PlaceOrder(market.symbol, user.user_id, user.user_name, reqOrders);
            // res.data.AddRange(aaa.data);
        }
        return res;
    }

}
