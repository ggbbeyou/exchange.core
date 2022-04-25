#!/bin/bash
echo 准备安装服务
cd /github/exchange.core
git pull
docker stop e_service
docker run --rm -v /github/exchange.core/:/app -w /app mcr.microsoft.com/dotnet/sdk:6.0 dotnet publish -c Release /app/Com.Service/Com.Service.csproj
docker start e_service
#docker run -d -p 8000:8000 --restart=always  --name e_service -v /github/exchange.core/Com.Service/bin/Release/net6.0/publish/:/app -w /app mcr.microsoft.com/dotnet/runtime:6.0 dotnet Com.Service.dll
echo 更新成功

