FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443   # важно! оставляем оба порта

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj ./
RUN dotnet restore
COPY . ./
WORKDIR /src
RUN dotnet build "NotifyBotMiniApp.csproj" -c Release --no-restore   # ← замени на своё имя!

FROM build AS publish
RUN dotnet publish "NotifyBotMiniApp.csproj" -c Release -o /app/publish --no-restore --no-build

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotifyBotMiniApp.dll"]   # ← тоже замени на своё