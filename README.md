# exchange.core
一个使用纯.net 5开发的开源交易所,有兴趣参与的伙伴请联系我。


1:Matching          撮合引擎    RabbitMQ
2:PushService       推送服务    RabbitMQ Websocket Redis
3:OrderGateway      订单网关    Websocket RabbitMQ
4:SerializeDb       存入数据库
5:Business          业务系统
6:Api               api接口
7:UI                UI
8:MarketMaking      作市机器人


基本流程

挂单流程
1:用户通过UI调用api挂单
2:订单网关