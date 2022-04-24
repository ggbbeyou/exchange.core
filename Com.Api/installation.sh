#!/bin/bash
echo 准备安装服务
cd /gitlab/exchange.core/Com.Api/
git pull
docker stop e_api
docker run --rm -v /gitlab/exchange.core/:/app -w /app mcr.microsoft.com/dotnet/sdk:6.0 dotnet publish -c Release /app/Com.Api/Com.Api.csproj
docker start e_api
#docker run -d -p 8002:80 --restart=always  --name e_api -v /gitlab/exchange.core/Com.Api/bin/Release/net6.0/publish/:/app -w /app mcr.microsoft.com/dotnet/aspnet:6.0 dotnet Com.Api.dll
echo 更新成功