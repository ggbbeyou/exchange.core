using System;
using System.Collections.Generic;
using Com.Model.Base;

namespace Com.Matching
{
    public class Test
    {
        string name = "btc/usdt";
        Core core = new Core("btc/usdt", null, null);

        public Test()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void AddOrder()
        {
            List<Order> orders = GetOrder();
            foreach (var item in orders)
            {
                List<Deal> deals = core.AddOrder(item);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Order> GetOrder()
        {
            List<Order> orders = new List<Order>();
            for (int i = 0; i < 10; i++)
            {
                Order order = new Order()
                {
                    id = Util.worker.NextId().ToString(),
                    name = this.name,
                    uid = i.ToString(),
                    price = 10 + i,
                    amount = 100,
                    total = (10 + i) * 100,
                    time = DateTimeOffset.UtcNow,
                    amount_unsold = 100,
                    amount_done = 0,
                    direction = i % 2 == 0 ? E_Direction.bid : E_Direction.ask,
                    state = E_DealState.unsold,
                    type = E_OrderType.price_market,
                    data = "什么玩意",
                };
                orders.Add(order);
            }
            for (int i = 0; i < 10; i++)
            {
                Order order = new Order()
                {
                    id = Util.worker.NextId().ToString(),
                    name = this.name,
                    uid = i.ToString(),
                    price = 10 + i,
                    amount = 100,
                    total = (10 + i) * 100,
                    time = DateTimeOffset.UtcNow,
                    amount_unsold = 100,
                    amount_done = 0,
                    direction = i % 2 == 0 ? E_Direction.bid : E_Direction.ask,
                    state = E_DealState.unsold,
                    type = E_OrderType.price_fixed,
                    data = "什么玩意",
                };
                orders.Add(order);
            }
            return orders;
        }


    }
}
