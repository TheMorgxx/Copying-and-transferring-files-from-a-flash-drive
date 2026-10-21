using System;
using System.Collections.Generic;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Paths
{
    public interface IPathResolver
    {
        // ambiguityResolver: получает список найденных кандидатов и возвращает выбранный путь
        // (или null, если пользователь отменил выбор). Вызывается только когда candidates.Count > 1.
        string Resolve(BackupItem item, Func<IReadOnlyList<string>, string> ambiguityResolver);
    }
}
