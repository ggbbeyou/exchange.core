using System;
using System.Collections.Generic;
using System.Diagnostics;
using Com.Common;
using Com.Model;
using Com.Model.Enum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Com.Matching
{
    public class Test
    {
        string name = "btc/usdt";
        Core core = null!;

        Random random = new Random();

        public Test(FactoryConstant constant)
        {
            core = new Core("btc/usdt");
            core.Start(43250);
            TestOrder();
        }

        public void TestOrder()
        {
            Console.WriteLine("Hello World!");
            List<MatchOrder> orders = GetOrder();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<Dealing> deals = AddOrder(orders);
            stopwatch.Stop();
            int count = deals.Count;
            Console.WriteLine($"order:{orders.Count},deals:{count},time:{stopwatch.Elapsed.TotalSeconds}秒,avg:{(stopwatch.Elapsed.TotalSeconds / count)}");
            Console.Read();
        }

        /// <summary>
        /// 
        /// </summary>
        public List<Dealing> AddOrder(List<MatchOrder> orders)
        {
            List<Dealing> deals = new List<Dealing>();
            for (int i = 0; i < orders.Count; i++)
            {
                deals.AddRange(core.Match(orders[i]));
            }
            return deals;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<MatchOrder> GetOrder()
        {
            List<MatchOrder> orders = new List<MatchOrder>();
            for (int i = 0; i < 500_000; i++)
            {
                E_OrderSide direction = random.Next(1, 3) == 1 ? E_OrderSide.buy : E_OrderSide.sell;
                E_OrderType type = random.Next(1, 3) == 1 ? E_OrderType.price_fixed : E_OrderType.price_market;
                // type = E_OrderType.price_fixed;
                decimal price = random.Next(45000, 45000 + 1);
                decimal amount = (decimal)random.NextDouble();
                if (type == E_OrderType.price_market)
                {
                    price = 0;
                }
                MatchOrder order = new MatchOrder()
                {
                    order_id = i,
                    market = this.name,
                    uid = i,
                    price = price,
                    amount = amount,
                    total = price * amount,
                    create_time = DateTimeOffset.UtcNow,
                    amount_unsold = amount,
                    amount_done = 0,
                    side = direction,
                    state = E_OrderState.unsold,
                    type = type,
                    data = "",
                };
                orders.Add(order);
            }
            return orders;
        }


    }
}
