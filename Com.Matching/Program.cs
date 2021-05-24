using System;
using System.Collections.Generic;
using Com.Model.Base;

namespace Com.Matching
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Test test = new Test();
            List<Order> orders = test.GetOrder();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            List<Deal> deals = test.AddOrder(orders);
            double time = (DateTimeOffset.UtcNow - now).TotalSeconds;
            int count = deals.Count;
            Console.WriteLine($"End ~~  count:{count},time:{time}秒,avg:{time / count}");
            Console.Read();
        }
    }
}
