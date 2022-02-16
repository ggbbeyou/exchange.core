# exchange.core
一个使用纯.net 5开发的开源交易所,有兴趣参与的伙伴请联系我。

项目说明:
Com.Api                 api接口
Com.Api.Model           api模型
Com.Api.Sdk             api接口SDK
Com.Bll                 业务逻辑
Com.Common              帮助通用类
Com.MarketMaking        作市机器人
Com.Matching            撮合引擎    RabbitMQ
Com.Model               基本模型
Com.UI                  UI
Com.Web                 后台管理网站
doc                     帮助文档
Com.PushService         推送服务    RabbitMQ Websocket Redis
Com.OrderGateway        订单网关    Websocket RabbitMQ
Com.SerializeDb         存入数据库
Com.Business            业务系统


基本流程

挂单流程
1:用户通过UI调用api挂单
2:订单网关


RabbitMQ配置
192.168.0.37
root
idcm@123

