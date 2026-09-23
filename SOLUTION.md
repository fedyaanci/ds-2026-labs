# Реализация PA4

PA4 продолжает решение PA3. После сохранения вычислений компоненты публикуют
доменные события в topic exchange `valuation-events`:

- `Valuator` публикует `SimilarityCalculated`;
- `RankCalculator` публикует `RankCalculated`.

Два экземпляра `EventsLogger` имеют отдельные очереди, привязанные к одному
exchange. Поэтому каждое событие доставляется обоим экземплярам, а не делится
между ними как задание для конкурирующих потребителей.

Запуск и просмотр логов:

```powershell
.\scripts\start.ps1
docker compose logs -f events-logger-1 events-logger-2
```

После отправки текста в логах обоих сервисов появятся тип события, ID текста и
значение метрики.
