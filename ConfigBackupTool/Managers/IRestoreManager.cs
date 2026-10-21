using System;
using System.Collections.Generic;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Managers
{
    public interface IRestoreManager
    {
        // ambiguityResolver: тот же контракт, что у IPathResolver.Resolve — получает список
        // кандидатов и возвращает выбранный (или null при отмене), вызывается только когда
        // кандидатов больше одного.
        //
        // manualRootResolver: вызывается один раз на игру, только если ВСЕ DriveRelative-элементы
        // этой игры дали 0 кандидатов при обычном резолве. Получает BackupItem первого
        // DriveRelative-элемента игры как подсказку (что искать), должен вернуть корневую папку
        // игры (gameRoot), выбранную пользователем вручную, или null при отмене.
        //
        // progress: необязательный канал построчного лога по ходу операции (для UI —
        // ScrolledText-подобная область лога, обновляется в реальном времени через
        // Task.Run + IProgress<string>). Может быть null — тогда лог доступен только
        // целиком в возвращаемом List<string> после завершения.
        List<string> Import(
            string manifestPath,
            string backupSessionFolder,
            string password,
            RestoreMode mode,
            Func<IReadOnlyList<string>, string> ambiguityResolver,
            Func<BackupItem, string> manualRootResolver,
            IProgress<string> progress = null);
    }
}
