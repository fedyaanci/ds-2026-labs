# Запуск PA6

Нужны Docker Desktop и PowerShell. Из корня репозитория:

```powershell
.\scripts\start.ps1
.\scripts\stop.ps1
```

Скрипт запускает четыре сегмента Redis, RabbitMQ, две копии Valuator, два конкурирующих
RankCalculator, два EventsLogger и Nginx. Приложение доступно на
`http://localhost:8080/`.

Проверить распределение заданий можно так:

```powershell
docker compose logs rank-calculator-1 rank-calculator-2
docker compose logs events-logger-1 events-logger-2
```
