using System;
using Com.Model.Base;
using Snowflake;

namespace Com.Matching
{
    /// <summary>
    /// RabbitMQ 接收数据和发送数据
    /// </summary>
    public class MQ
    {
        private Core core;

        public MQ(Core core)
        {
            this.core = core;
        }

    }

}