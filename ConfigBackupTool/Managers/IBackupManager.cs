using System;
using System.Collections.Generic;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Managers
{
    public interface IBackupManager
    {
        // progress: необязательный канал построчного лога по ходу операции (для UI —
        // ScrolledText-подобная область лога, обновляется в реальном времени через
        // Task.Run + IProgress<string>). Может быть null — тогда лог доступен только
        // целиком в возвращаемом List<string> после завершения.
        List<string> Export(List<GameDefinition> games, string destinationFolder, string password, IProgress<string> progress = null);
    }
}
