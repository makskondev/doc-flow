namespace DocFlow.Documents;

/// <summary>Ключи для обмена данными между шагами (DocumentProcess.Bag).</summary>
public static class BagKeys
{
    public const string Sha256 = "sha256";               // хеш PDF, который мы отправили на подпись
    public const string PdfPath = "pdfPath";             // где сейчас лежит PDF
    public const string SigPath = "sigPath";             // где сейчас лежит подпись
    public const string FileName = "fileName";           // исходное имя файла (под ним файл ляжет в конечный каталог)
    public const string ExternalDocId = "externalDocId"; // идентификатор документа во внешней БД (если его выдала регистрация)
}
