# Запуск PA3

Нужны Docker Desktop и PowerShell. Из корня репозитория:

```powershell
.\scripts\start.ps1
.\scripts\stop.ps1
```

Скрипт запускает Redis, RabbitMQ, две копии Valuator, два конкурирующих
RankCalculator и Nginx. Приложение доступно на `http://localhost:8080/`.

Проверить распределение заданий можно так:

```powershell
docker compose logs rank-calculator-1 rank-calculator-2
```
