# Реализация PA8

## Компоненты

- `ProtoKey` — HTTP key-value сервис на `http://127.0.0.1:7777`.
- `ProtoCli` — консольный клиент для команд `set`, `get`, `keys`.
- `ProtoKey.Tests` — тесты ограничений на ключи и префиксы.

## API

| Операция | HTTP |
|---|---|
| `set(key, value)` | `POST /set` с JSON `{ "key": "name", "value": 17 }` |
| `get(key)` | `GET /get/name` |
| `keys(prefix)` | `GET /keys?prefix=na` |

`Dictionary<string, int>` принадлежит только `StoreWorker`. HTTP-обработчики не
трогают словарь напрямую: они записывают команду в `Channel<StoreCommand>` и
асинхронно ждут `TaskCompletionSource`. Поэтому изменение словаря всегда
выполняется одним потоком чтения очереди.

Успешные команды `set` копируются во второй Channel. `PersistenceWorker` раз в
секунду добавляет накопившиеся команды в `ProtoKey.data`. При старте
`StoreWorker` проигрывает журнал и восстанавливает последнее значение ключей.

## Запуск

В первом терминале:

```powershell
dotnet run --project .\ProtoKey\ProtoKey.csproj
```

Во втором терминале:

```powershell
dotnet run --project .\ProtoCli\ProtoCli.csproj -- set requests_total 17
dotnet run --project .\ProtoCli\ProtoCli.csproj -- get requests_total
dotnet run --project .\ProtoCli\ProtoCli.csproj -- keys req
```

Тесты:

```powershell
dotnet test .\ds-2026.slnx
```
