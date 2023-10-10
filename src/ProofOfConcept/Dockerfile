FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Bot/MessageMediator.Bot.csproj", "src/Bot/"]
RUN dotnet restore "src/Bot/MessageMediator.Bot.csproj"
COPY . .
WORKDIR "/src/src/Bot"
RUN dotnet build "MessageMediator.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MessageMediator.Bot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessageMediator.Bot.dll"]
