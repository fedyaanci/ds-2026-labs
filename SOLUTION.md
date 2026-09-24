# Реализация PA6

В форме появился выбор страны. Страна однозначно задаёт ключ сегментирования:

| Страна | Сегмент |
|---|---|
| Russia | RU |
| France, Germany | EU |
| UAE, India | ASIA |

В Docker Compose запускаются четыре независимых Redis:

- `redis-main` хранит только пары `SHARD-{id} → регион`;
- `redis-ru`, `redis-eu`, `redis-asia` хранят тексты, страны и результаты.

`Valuator` при записи сразу знает регион из страны. `Summary` и
`RankCalculator` сначала читают карту из `DB_MAIN`, затем получают нужное
соединение через `ShardedRedisStore`. Каждое такое обращение пишет лог
`LOOKUP: {id}, {region}`.

Запуск:

```powershell
.\scripts\start.ps1
docker compose logs -f valuator-1 rank-calculator-1
```

Проверить размещение данных можно командами:

```powershell
docker compose exec redis-main redis-cli KEYS "SHARD-*"
docker compose exec redis-ru redis-cli KEYS "TEXT-*"
docker compose exec redis-eu redis-cli KEYS "TEXT-*"
docker compose exec redis-asia redis-cli KEYS "TEXT-*"
```
