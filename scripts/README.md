# Запуск PA7

Нужны Docker Desktop и PowerShell. Из корня репозитория:

```powershell
Copy-Item .env.example .env
```

При необходимости измените локальные пароли в `.env`, затем:

```powershell
.\scripts\start.ps1
.\scripts\stop.ps1
```

Скрипт запускает защищённые паролем Redis и RabbitMQ, две копии Valuator, два конкурирующих
RankCalculator, два EventsLogger и Nginx. Приложение доступно на
`http://localhost:8080/`.

Проверить распределение заданий можно так:

```powershell
docker compose logs rank-calculator-1 rank-calculator-2
docker compose logs events-logger-1 events-logger-2
```
