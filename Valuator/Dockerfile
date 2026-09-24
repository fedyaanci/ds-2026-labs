FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["ShardStore/ShardStore.csproj", "ShardStore/"]
COPY ["Valuator/Valuator.csproj", "Valuator/"]
RUN dotnet restore "Valuator/Valuator.csproj"
COPY . .
RUN dotnet publish "Valuator/Valuator.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Valuator.dll"]
