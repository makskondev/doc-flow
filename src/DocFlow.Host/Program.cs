// Точка сборки сервиса. Здесь только подключение слоёв; логики нет.
//   DocFlow.Data       — хранилище (PostgreSQL, EF Core)
//   DocFlow.Engine     — движок: маршруты, процессы, воркеры
//   DocFlow.Documents  — бизнес-логика: типы документов, маршруты, шаги
//   DocFlow.Host       — инфраструктура (источники, файлы, внешние системы), операторский API
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Порядок важен: хранилище — до движка (репозиторий должен быть зарегистрирован раньше, миграции — выполниться до воркеров)
builder.Services.AddDocFlowPostgres(config);
builder.Services.AddDocFlowEngine(config);
builder.Services.AddDocFlowDocuments(config);
builder.Services.AddDocFlowHostInfrastructure(config);

var app = builder.Build();
app.MapOperatorApi();
app.Run();
