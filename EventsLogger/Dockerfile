FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Contracts/Contracts.csproj", "Contracts/"]
COPY ["EventsLogger/EventsLogger.csproj", "EventsLogger/"]
RUN dotnet restore "EventsLogger/EventsLogger.csproj"
COPY . .
RUN dotnet publish "EventsLogger/EventsLogger.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventsLogger.dll"]
