using System;
using System.Collections.Generic;
using Com.Model.Base;
using Newtonsoft.Json;

namespace Com.Matching
{
    public class Test
    {
        string name = "btc/usdt";
        Core core = new Core("btc/usdt", null, null);

        Random random = new Random();

        public Test()
        {
            core.Start(10);
        }

        /// <summary>
        /// 
        /// </summary>
        public List<Deal> AddOrder()
        {
            List<Deal> deals = new List<Deal>();
            List<Order> orders = GetOrder();
            foreach (var item in orders)
            {
                deals.AddRange(core.Match(item));
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
            for (int i = 0; i < 10000; i++)
            {
                decimal price = random.Next(1, 50) + (decimal)random.NextDouble();
                decimal amount = random.Next(1, 10) + (decimal)random.NextDouble();
                E_Direction direction = random.Next() % 2 == 0 ? E_Direction.bid : E_Direction.ask;
                E_OrderType type = random.Next() % 2 == 0 ? E_OrderType.price_fixed : E_OrderType.price_market;
                if (type == E_OrderType.price_market)
                {
                    price = 0;
                }
                Order order = new Order()
                {
                    id = Util.worker.NextId().ToString(),
                    name = this.name,
                    uid = i.ToString(),
                    price = price,
                    amount = amount,
                    total = price * amount,
                    time = DateTimeOffset.UtcNow,
                    amount_unsold = amount,
                    amount_done = 0,
                    direction = direction,
                    state = E_DealState.unsold,
                    type = type,
                    data = "",
                };
                orders.Add(order);
            }
            return orders;
        }


    }
}
