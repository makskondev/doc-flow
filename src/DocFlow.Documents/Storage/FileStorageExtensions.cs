namespace DocFlow.Documents;

public static class FileStorageExtensions
{
    /// <summary>
    /// Перенос файла, безопасный к повтору шага. Источника нет, а результат есть — уже перенесено на прошлой попытке.
    /// Если файл есть и там и там: при <paramref name="overwriteDifferent"/> = false одинаковое содержимое считается
    /// недозавершённым прошлым переносом (источник удаляется), а другое содержимое — чужим файлом с тем же именем (эскалация);
    /// при true целевой файл перезаписывается.
    /// </summary>
    public static async Task MoveIdempotentAsync(
        this IFileStorage fs, string source, string destination, bool overwriteDifferent, CancellationToken ct)
    {
        if (source == destination) return;

        var sourceExists = await fs.ExistsAsync(source, ct);
        var destinationExists = await fs.ExistsAsync(destination, ct);

        if (!sourceExists)
        {
            if (destinationExists) return;
            throw new FileNotFoundException($"Нет ни источника, ни результата: {source}");
        }

        if (destinationExists && !overwriteDifferent)
        {
            var same = Hashing.Sha256Hex(await fs.ReadAsync(source, ct)) == Hashing.Sha256Hex(await fs.ReadAsync(destination, ct));
            if (!same)
                throw new PermanentStepException($"В каталоге назначения уже есть другой файл с таким именем: {destination}");
            await fs.DeleteAsync(source, ct);
            return;
        }

        await fs.MoveAsync(source, destination, overwrite: true, ct);
    }
}
