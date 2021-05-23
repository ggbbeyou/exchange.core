using System;

namespace Com.Matching
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            DateTimeOffset now = DateTimeOffset.UtcNow;
            Test test = new Test();
            Console.WriteLine(test.AddOrder().Count);

            Console.WriteLine("End ~~ " + (DateTimeOffset.UtcNow - now).TotalSeconds);
            Console.Read();
        }
    }
}
