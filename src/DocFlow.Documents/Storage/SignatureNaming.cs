namespace DocFlow.Documents;

/// <summary>Как называется файл подписи относительно файла документа (соглашение системы подписания).</summary>
public enum SignatureNaming
{
    /// <summary>К имени файла добавляется «.sig»: «договор.pdf» → «договор.pdf.sig».</summary>
    AppendSuffix,

    /// <summary>Расширение заменяется: «договор.pdf» → «договор.sig».</summary>
    ReplaceExtension
}
