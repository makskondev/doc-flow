# DocFlow

Сервис, который описывает **процесс движения документа** как маршрут из шагов и сам его выполняет: хранит состояние
в PostgreSQL, ждёт внешних событий (файл подписи), повторяет сбойные шаги, эскалирует оператору.

## Где что лежит

```
src/
  DocFlow.Documents/   ◄── ЗДЕСЬ РАБОТАЕТ РАЗРАБОТЧИК: типы документов, маршруты, шаги
    Zop/ Report/ Inbox/ Archive/   по папке на тип документа: маршрут (с правилом Matches) и его специфичные шаги
    Views/               описания вьюх сторонней БД (источники документов)
    Common/              общие шаги, фрагменты маршрутов, сканер результата подписания
    Storage/ Integrations/   контракты: файлы, внешние системы
  DocFlow.Host/        точка сборки (Program.cs), операторский API, инфраструктура
    Infrastructure/      источник «каталоги», файловая система, письма
    Infrastructure/Stubs/    ЗАГЛУШКИ внешних систем — заменить реальными реализациями
  DocFlow.Engine/      движок общего назначения, знаний о документах не имеет
    Processes/ Routing/ Steps/ Intake/ Execution/ Workers/ Persistence/ ...
  DocFlow.Data/        хранилище: PostgreSQL + EF Core (DbContext, репозиторий, миграции)
tests/DocFlow.Tests/   тесты движка (Support/Harness — заготовка для новых тестов)
docs/                  документация
```

Зависимости: `Host → Documents → Engine`, `Host → Data → Engine`. Engine ни от чего из них не зависит.

| Что нужно сделать | Куда идти |
|---|---|
| Добавить тип документа, шаг, ожидание | `docs/adding-a-document-type.md`, папка `DocFlow.Documents` |
| Понять, как всё работает | `docs/architecture.md` |
| Запустить, настроить, разобрать эскалацию | `docs/operations.md` |
| Узнать, что осталось до продакшена | `docs/before-production.md` |

## Быстрый старт

```bash
docker compose up -d                       # локальная PostgreSQL (или своя: ConnectionStrings:DocFlow)
dotnet tool install --global dotnet-ef     # один раз
dotnet build
dotnet test

# Первая миграция (папка Migrations появится в src/DocFlow.Data; её нужно закоммитить)
dotnet ef migrations add InitialCreate --project src/DocFlow.Data --startup-project src/DocFlow.Host

dotnet run --project src/DocFlow.Host      # миграции применятся при старте
```

Версия платформы — `Directory.Build.props` (`net10.0`), версии пакетов — `Directory.Packages.props`.
Договорённости по коду — `.editorconfig` (один namespace на проект, папки — для навигации).
