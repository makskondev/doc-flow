namespace DocFlow.Documents;

/// <summary>
/// Типы документов = ключи маршрутов. Примеры соответствуют четырём сценариям постановки — переименуйте под свои типы.
/// Какой документ к какому типу относится, решает правило Matches в маршруте.
/// </summary>
public static class DocumentTypes
{
    public const string Zop = "zop";           // сценарий 1: вьюха view_1, файл подписывает пользователь (со штампом)
    public const string Report = "report";     // сценарий 2: вьюха view_2, автоподпись в REST-сервисе
    public const string Inbox = "inbox";       // сценарий 3: файл появился в каталоге, запись о нём в БД, автоподпись
    public const string Archive = "archive";   // сценарий 4: вьюха view_1, без подписи: найти файл и переложить
}
