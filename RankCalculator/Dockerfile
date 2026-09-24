FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["ShardStore/ShardStore.csproj", "ShardStore/"]
COPY ["RankCalculator/RankCalculator.csproj", "RankCalculator/"]
RUN dotnet restore "RankCalculator/RankCalculator.csproj"
COPY . .
RUN dotnet publish "RankCalculator/RankCalculator.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RankCalculator.dll"]
