#!/bin/bash
echo 准备安装服务
cd /github/exchange.core/Com.Api/
git pull
docker stop api
docker run --rm -v /github/exchange.core/:/app -w /app mcr.microsoft.com/dotnet/sdk:6.0 dotnet publish -c Release /app/Com.Api/Com.Api.csproj
docker start api
#docker run -d -p 8001:80 --restart=always  --name api -v /github/exchange.core/Com.Api/bin/Release/net6.0/publish/:/app -w /app mcr.microsoft.com/dotnet/aspnet:6.0 dotnet Com.Api.dll
echo 更新成功