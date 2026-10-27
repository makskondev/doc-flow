using System.Text.RegularExpressions;

namespace DocFlow.Documents;

/// <summary>Сопоставление имени файла с маской (* и ?) для правил Matches. Регистр не учитывается.</summary>
public static class FileMask
{
    public static bool IsMatch(string fileName, string mask)
        => Regex.IsMatch(
            fileName,
            "^" + Regex.Escape(mask).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
}
