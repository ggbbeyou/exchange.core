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
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Test test = new Test();
            List<Deal> deals = test.AddOrder();

            int count = deals.Count;
            double time = (DateTimeOffset.UtcNow - now).TotalSeconds;

            Console.WriteLine($"End ~~  count:{count},time:{time}秒,avg:{time / count}");
            Console.Read();
        }
    }
}
