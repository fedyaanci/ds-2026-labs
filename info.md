1 )
term:

dotnet new webapp -o myApp

dotnet watch

struct:

Program.cs - настр-ка и запуск приложухи

Pagees/Index.cshtml - html главной страницы

Pages/Index.cshtml.cs -cs логика главнйо страницы

Pages/Shared /_Layout.cshtml - каркас сайта

wwwroot - css, js и фото

2 )
NuGet-библиотека подключается к конкретному файлу проекта .csproj

зайти в каталог где находится csproj и 

dotnet add package Newtonsoft.Json

появится типо такой темки в csproj
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>

3 ) до круга в вс код

Задание:

dotnet run --project .\Valuator\Valuator.csproj

докер запускает изол контейнеры-проги
редис -бд, зашитая внутри контейнера -> то есть запускается вместе с докером
asp.net прилож-е подключается к редис по какому то браузеру

docker run --name valuator-redis -p 6379:6379 -d redis
docker exec valuator-redis redis-cli ping -проверка 

1 этап
dotnet add package StackExchange.Redis  - установка библиотеки
dotnet list package - првоерка

2 этап

// указание адреса Redis в настройках проекта 
appsettings.json
  "ConnectionStrings": { 
    "Redis": "localhost:6379"
  }

привет это я 
docker exec valuator-redis redis-cli GET "TEXT-b9a9dec9-f3bc-47fa-975a-a2689cabd420"