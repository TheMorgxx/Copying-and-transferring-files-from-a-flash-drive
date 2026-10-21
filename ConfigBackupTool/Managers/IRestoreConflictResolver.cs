using System;

namespace ConfigBackupTool.Managers
{
    public enum RestoreConflictAction
    {
        Replace,
        Keep
    }

    public interface IRestoreConflictResolver
    {
        // Вызывается только когда target существует и его текущий хэш отличается от OriginalHash
        // из манифеста (RestoreMode.AskOnConflict). currentLastWriteTime/currentFileSize — метаданные
        // текущего файла на диске, чтобы диалог мог показать пользователю осмысленную подсказку
        // (например "текущий файл новее бэкапа" или "текущий файл больше").
        RestoreConflictAction Resolve(string gameName, string targetPath, DateTime currentLastWriteTime, long currentFileSize);
    }
}
