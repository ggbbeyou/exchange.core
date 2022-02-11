using System;
using System.Collections.Generic;
using Com.Model;
using Com.Model.Enum;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Com.Matching
{
    public class Test
    {
        string name = "btc/usdt";
        Core? core = null;

        Random random = new Random();

        public Test(IConfiguration configuration)
        {
            core = new Core("btc/usdt", configuration, null);
            core.Start(45);
        }

        public void TestOrder()
        {
            Console.WriteLine("Hello World!");
            List<Order> orders = GetOrder();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            List<Deal> deals = AddOrder(orders);
            double time = (DateTimeOffset.UtcNow - now).TotalSeconds;
            int count = deals.Count;
            Console.WriteLine($"End ~~  count:{count},time:{time}秒,avg:{(time / count)}");
            Console.Read();
        }

        /// <summary>
        /// 
        /// </summary>
        public List<Deal> AddOrder(List<Order> orders)
        {
            List<Deal> deals = new List<Deal>();
            for (int i = 0; i < orders.Count; i++)
            {
                //deals.AddRange(core.Match(orders[i]));
                core!.SendOrder(orders[i]);
                //deals.AddRange(core.Process(orders[i]));
            }

            return deals;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Order> GetOrder()
        {
            List<Order> orders = new List<Order>();
            for (int i = 0; i < 100000; i++)
            {
                E_OrderSide direction = random.Next(1, 3) == 1 ? E_OrderSide.buy : E_OrderSide.sell;
                E_OrderType type = random.Next(1, 3) == 1 ? E_OrderType.price_fixed : E_OrderType.price_market;
                type = E_OrderType.price_fixed;
                decimal price = random.Next(50, 100);
                decimal amount = random.Next(50, 100);
                if (type == E_OrderType.price_market)
                {
                    price = 0;
                }
                Order order = new Order()
                {
                    id = i.ToString(),
                    name = this.name,
                    uid = i.ToString(),
                    price = price,
                    amount = amount,
                    total = price * amount,
                    time = DateTimeOffset.UtcNow,
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
