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
        public List<Order> GetOrder()
        {
            List<Order> orders = new List<Order>();
            for (int i = 0; i < 1000000; i++)
            {
                E_Direction direction = random.Next(1, 3) == 1 ? E_Direction.bid : E_Direction.ask;
                E_OrderType type = random.Next(1, 3) == 1 ? E_OrderType.price_fixed : E_OrderType.price_market;
                decimal price = 0;
                decimal amount = random.Next(1, 10);
                if (type == E_OrderType.price_fixed)
                {
                    if (direction == E_Direction.bid)
                    {
                        price = random.Next(40, 50);
                    }
                    else
                    {
                        price = random.Next(40, 50);
                    }
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
