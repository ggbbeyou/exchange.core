#!/bin/bash
echo 准备安装服务
cd /gitlab/exchange.core/Com.Api.Admin/
git pull
docker stop e_admin
docker run --rm -v /gitlab/exchange.core/:/app -w /app mcr.microsoft.com/dotnet/sdk:6.0 dotnet publish -c Release /app/Com.Api.Admin/Com.Api.Admin.csproj
docker start e_admin
#docker run -d -p 8001:80 --restart=always  --name e_admin -v /gitlab/exchange.core/Com.Api.Admin/bin/Release/net6.0/publish/:/app -w /app mcr.microsoft.com/dotnet/aspnet:6.0 dotnet Com.Api.Admin.dll
echo 更新成功