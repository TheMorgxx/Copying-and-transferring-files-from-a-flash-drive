using System;
using System.Collections.Generic;
using System.IO;
using ConfigBackupTool.Domain;
using ConfigBackupTool.Encryption;
using ConfigBackupTool.Manifest;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.Managers
{
    public class RestoreManager : IRestoreManager
    {
        private const string SafetyBackupRoot = "C:/ConfigBackups";

        private readonly IPathResolver _pathResolver;
        private readonly IEncryptionService _encryptionService;
        private readonly IBackupManifestRepository _manifestRepository;
        private readonly IRestoreConflictResolver _conflictResolver;

        public RestoreManager(IPathResolver pathResolver, IEncryptionService encryptionService, IBackupManifestRepository manifestRepository, IRestoreConflictResolver conflictResolver)
        {
            _pathResolver = pathResolver;
            _encryptionService = encryptionService;
            _manifestRepository = manifestRepository;
            _conflictResolver = conflictResolver;
        }

        public List<string> Import(
            string manifestPath,
            string backupSessionFolder,
            string password,
            RestoreMode mode,
            Func<IReadOnlyList<string>, string> ambiguityResolver,
            Func<BackupItem, string> manualRootResolver,
            IProgress<string> progress = null)
        {
            List<string> log = new List<string>();
            Domain.BackupManifest manifest = _manifestRepository.Load(manifestPath, password);
            string restoreTimestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

            for (int i = 0; i < manifest.Games.Count; i++)
            {
                GameManifestEntry gameEntry = manifest.Games[i];
                ImportGame(gameEntry, backupSessionFolder, password, mode, ambiguityResolver, manualRootResolver, restoreTimestamp, log, progress);
            }

            return log;
        }

        private void ImportGame(
            GameManifestEntry gameEntry,
            string backupSessionFolder,
            string password,
            RestoreMode mode,
            Func<IReadOnlyList<string>, string> ambiguityResolver,
            Func<BackupItem, string> manualRootResolver,
            string restoreTimestamp,
            List<string> log,
            IProgress<string> progress)
        {
            bool isPresent = DetectGamePresence(gameEntry);

            if (isPresent == false)
            {
                Report(log, progress, gameEntry.GameName + ": не установлена, пропущена");
                return;
            }

            string gameRoot = EstablishGameRoot(gameEntry, ambiguityResolver, manualRootResolver, log, progress);

            for (int i = 0; i < gameEntry.Items.Count; i++)
            {
                ManifestItem item = gameEntry.Items[i];
                ImportItem(gameEntry.GameName, item, backupSessionFolder, password, mode, gameRoot, restoreTimestamp, log, progress);
            }

            Report(log, progress, gameEntry.GameName + ": восстановлена");
        }

        private bool DetectGamePresence(GameManifestEntry gameEntry)
        {
            for (int i = 0; i < gameEntry.Items.Count; i++)
            {
                ManifestItem item = gameEntry.Items[i];
                BackupItem backupItem = ToBackupItem(gameEntry.GameName, item);

                string silentResult = _pathResolver.Resolve(backupItem, SilentAmbiguityResolver);
                if (silentResult != null)
                {
                    return true;
                }
            }

            return false;
        }

        private string SilentAmbiguityResolver(IReadOnlyList<string> candidates)
        {
            return candidates[0];
        }

        private string EstablishGameRoot(
            GameManifestEntry gameEntry,
            Func<IReadOnlyList<string>, string> ambiguityResolver,
            Func<BackupItem, string> manualRootResolver,
            List<string> log,
            IProgress<string> progress)
        {
            List<ManifestItem> driveRelativeItems = new List<ManifestItem>();

            for (int i = 0; i < gameEntry.Items.Count; i++)
            {
                if (gameEntry.Items[i].Anchor == AnchorType.DriveRelative)
                {
                    driveRelativeItems.Add(gameEntry.Items[i]);
                }
            }

            if (driveRelativeItems.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < driveRelativeItems.Count; i++)
            {
                ManifestItem item = driveRelativeItems[i];
                BackupItem backupItem = ToBackupItem(gameEntry.GameName, item);
                string resolved = _pathResolver.Resolve(backupItem, ambiguityResolver);

                if (resolved != null)
                {
                    return StripTail(resolved, item.Tail);
                }
            }

            if (manualRootResolver != null)
            {
                BackupItem referenceItem = ToBackupItem(gameEntry.GameName, driveRelativeItems[0]);
                string manualRoot = manualRootResolver(referenceItem);

                if (manualRoot != null)
                {
                    return manualRoot;
                }
            }

            Report(log, progress, gameEntry.GameName + ": не удалось определить корневую папку — элементы с якорем DriveRelative будут пропущены");
            return null;
        }

        private void ImportItem(
            string gameName,
            ManifestItem item,
            string backupSessionFolder,
            string password,
            RestoreMode mode,
            string gameRoot,
            string restoreTimestamp,
            List<string> log,
            IProgress<string> progress)
        {
            string targetPath = ResolveTargetPath(item, gameRoot);

            if (targetPath == null)
            {
                Report(log, progress, gameName + ": не удалось определить путь для " + item.Tail + " — пропущен");
                return;
            }

            if (item.EntryType == BackupEntryType.File)
            {
                ImportFile(gameName, targetPath, item.Anchor, item.Tail, item.EncryptedFileName, item.Salt, item.IV, item.OriginalHash, backupSessionFolder, password, mode, restoreTimestamp, log, progress);
            }
            else
            {
                ImportDirectory(gameName, targetPath, item.Anchor, item.Tail, item.Files, backupSessionFolder, password, mode, restoreTimestamp, log, progress);
            }
        }

        private string ResolveTargetPath(ManifestItem item, string gameRoot)
        {
            if (item.Anchor == AnchorType.UserProfile)
            {
                return CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), item.Tail);
            }

            if (item.Anchor == AnchorType.AppDataRoaming)
            {
                return CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), item.Tail);
            }

            if (gameRoot == null)
            {
                return null;
            }

            return CombinePath(gameRoot, item.Tail);
        }

        private void ImportDirectory(
            string gameName,
            string targetDirectory,
            AnchorType anchor,
            string directoryTail,
            List<ManifestFileEntry> files,
            string backupSessionFolder,
            string password,
            RestoreMode mode,
            string restoreTimestamp,
            List<string> log,
            IProgress<string> progress)
        {
            if (files == null)
            {
                return;
            }

            for (int i = 0; i < files.Count; i++)
            {
                ManifestFileEntry fileEntry = files[i];
                string targetPath = CombinePath(targetDirectory, fileEntry.RelativePath);

                // Относительный путь файла внутри папки игры для safety-бэкапа: Tail папки + RelativePath файла
                string fileTail = CombinePath(directoryTail, fileEntry.RelativePath);

                ImportFile(gameName, targetPath, anchor, fileTail, fileEntry.EncryptedFileName, fileEntry.Salt, fileEntry.IV, fileEntry.OriginalHash, backupSessionFolder, password, mode, restoreTimestamp, log, progress);
            }
        }

        private void ImportFile(
            string gameName,
            string targetPath,
            AnchorType anchor,
            string relativePath,
            string encryptedFileName,
            string saltBase64,
            string ivBase64,
            string originalHash,
            string backupSessionFolder,
            string password,
            RestoreMode mode,
            string restoreTimestamp,
            List<string> log,
            IProgress<string> progress)
        {
            bool targetExists = File.Exists(targetPath);

            if (targetExists == true)
            {
                string currentHash;

                try
                {
                    byte[] currentBytes = File.ReadAllBytes(targetPath);
                    currentHash = _encryptionService.ComputeHash(currentBytes);
                }
                catch (Exception ex)
                {
                    Report(log, progress, gameName + ": не удалось прочитать текущий файл " + targetPath + ": " + ex.Message + " — пропущен");
                    return;
                }

                if (currentHash == originalHash)
                {
                    return;
                }

                bool shouldReplace = DecideConflict(gameName, targetPath, mode, log, progress);

                if (shouldReplace == false)
                {
                    return;
                }

                bool backupSucceeded = SafetyBackup(gameName, targetPath, anchor, relativePath, restoreTimestamp, log, progress);

                if (backupSucceeded == false)
                {
                    return;
                }
            }

            try
            {
                WriteDecryptedFile(targetPath, encryptedFileName, saltBase64, ivBase64, originalHash, backupSessionFolder, password);
                Report(log, progress, gameName + ": " + targetPath + (targetExists == true ? " заменён" : " восстановлен"));
            }
            catch (Exception ex)
            {
                Report(log, progress, gameName + ": ошибка при восстановлении " + targetPath + ": " + ex.Message + " — пропущен");
            }
        }

        private bool DecideConflict(string gameName, string targetPath, RestoreMode mode, List<string> log, IProgress<string> progress)
        {
            if (mode == RestoreMode.AddMissingOnly)
            {
                Report(log, progress, gameName + ": " + targetPath + " уже существует и отличается — пропущен (AddMissingOnly)");
                return false;
            }

            if (mode == RestoreMode.ReplaceAll)
            {
                return true;
            }

            FileInfo fileInfo = new FileInfo(targetPath);
            RestoreConflictAction action = _conflictResolver.Resolve(gameName, targetPath, fileInfo.LastWriteTimeUtc, fileInfo.Length);

            if (action == RestoreConflictAction.Keep)
            {
                Report(log, progress, gameName + ": " + targetPath + " оставлен без изменений (выбор пользователя)");
                return false;
            }

            return true;
        }

        // Возвращает false при неудаче — вызывающий код обязан отменить замену файла в этом случае:
        // без успешного safety-бэкапа заменять оригинал нельзя (обесценивает саму идею бэкапа).
        //
        // Структура: <SafetyBackupRoot>/<timestamp>/<GameName (очищенное)>/<Anchor>/<Tail (очищенный)>.
        // Так два разных options.txt (разные Tail или разные Anchor) не сливаются в один плоский файл.
        private bool SafetyBackup(string gameName, string targetPath, AnchorType anchor, string relativePath, string restoreTimestamp, List<string> log, IProgress<string> progress)
        {
            try
            {
                string safeGameName = SanitizePathSegment(gameName);
                if (safeGameName.Length == 0)
                {
                    safeGameName = "_";
                }

                string safeRelativePath = SanitizeRelativePath(relativePath);
                if (safeRelativePath.Length == 0)
                {
                    // Tail целиком оказался пустым после очистки — откатываемся к имени файла
                    safeRelativePath = SanitizePathSegment(Path.GetFileName(targetPath));

                    if (safeRelativePath.Length == 0)
                    {
                        safeRelativePath = "_";
                    }
                }

                string plannedPath = SafetyBackupRoot + "/" + restoreTimestamp + "/" + safeGameName + "/" + anchor.ToString() + "/" + safeRelativePath;

                string backupFolder = Path.GetDirectoryName(plannedPath);
                if (string.IsNullOrEmpty(backupFolder) == false)
                {
                    Directory.CreateDirectory(backupFolder);
                }

                string backupPath = EnsureUniquePath(plannedPath);
                File.Copy(targetPath, backupPath, false);
                return true;
            }
            catch (Exception ex)
            {
                Report(log, progress, gameName + ": не удалось сделать safety-бэкап " + targetPath + ": " + ex.Message + " — файл НЕ заменён");
                return false;
            }
        }

        // Очищает относительный путь (Tail / RelativePath из манифеста) для использования внутри
        // папки safety-бэкапа: выбрасывает пустые сегменты, "." и "..", заменяет недопустимые
        // символы. Без этого ".." в манифесте вывел бы бэкап за пределы своей папки.
        private string SanitizeRelativePath(string relativePath)
        {
            if (relativePath == null)
            {
                return string.Empty;
            }

            string[] segments = relativePath.Replace("\\", "/").Split('/');
            List<string> cleanSegments = new List<string>();

            for (int i = 0; i < segments.Length; i++)
            {
                string cleanSegment = SanitizePathSegment(segments[i]);

                if (cleanSegment.Length > 0)
                {
                    cleanSegments.Add(cleanSegment);
                }
            }

            return string.Join("/", cleanSegments);
        }

        // Очищает один сегмент пути: недопустимые символы -> "_", хвостовые точки и пробелы
        // отбрасываются (Windows их всё равно отрезает), "." и ".." в результате дают пустую строку.
        // Пустая строка означает "сегмент надо пропустить".
        private string SanitizePathSegment(string segment)
        {
            if (segment == null)
            {
                return string.Empty;
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] chars = segment.Trim().ToCharArray();

            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalidChars, chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            return new string(chars).TrimEnd(' ', '.');
        }

        // Порядок: расшифровка -> сверка SHA256 с OriginalHash -> запись во временный файл рядом
        // с целевым -> замена целевого через File.Move(overwrite). При любой ошибке (в том числе
        // несовпадении хэша) целевой файл остаётся нетронутым, временный файл удаляется.
        private void WriteDecryptedFile(string targetPath, string encryptedFileName, string saltBase64, string ivBase64, string originalHash, string backupSessionFolder, string password)
        {
            string sourcePath = CombinePath(backupSessionFolder, encryptedFileName);
            byte[] cipherBytes = File.ReadAllBytes(sourcePath);
            byte[] salt = Convert.FromBase64String(saltBase64);
            byte[] iv = Convert.FromBase64String(ivBase64);

            byte[] plainBytes = _encryptionService.Decrypt(cipherBytes, salt, iv, password);

            string actualHash = _encryptionService.ComputeHash(plainBytes);
            bool hashMatches = originalHash != null && string.Equals(actualHash, originalHash, StringComparison.Ordinal) == true;

            if (hashMatches == false)
            {
                throw new InvalidOperationException("хэш расшифрованных данных не совпадает с манифестом, файл не записан");
            }

            string targetDirectory = Path.GetDirectoryName(targetPath);
            if (string.IsNullOrEmpty(targetDirectory) == false)
            {
                Directory.CreateDirectory(targetDirectory);
            }

            string tempPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                File.WriteAllBytes(tempPath, plainBytes);
                File.Move(tempPath, targetPath, true);
            }
            catch (Exception)
            {
                try
                {
                    if (File.Exists(tempPath) == true)
                    {
                        File.Delete(tempPath);
                    }
                }
                catch (Exception)
                {
                    // Не удалось удалить временный файл — не маскируем исходную ошибку записи
                }

                throw;
            }
        }

        private string EnsureUniquePath(string path)
        {
            if (File.Exists(path) == false)
            {
                return path;
            }

            string directory = Path.GetDirectoryName(path).Replace("\\", "/");
            string fileNameNoExt = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            int counter = 1;
            string candidate;

            do
            {
                candidate = directory + "/" + fileNameNoExt + "_" + counter + extension;
                counter++;
            }
            while (File.Exists(candidate) == true);

            return candidate;
        }

        private BackupItem ToBackupItem(string gameName, ManifestItem item)
        {
            BackupItem backupItem = new BackupItem();
            backupItem.GameName = gameName;
            backupItem.Anchor = item.Anchor;
            backupItem.Tail = item.Tail;
            backupItem.OriginalPath = item.OriginalPath;
            backupItem.OriginalDrive = item.OriginalDrive;
            backupItem.EntryType = item.EntryType;
            return backupItem;
        }

        private string StripTail(string resolvedPath, string tail)
        {
            string normalizedResolved = resolvedPath.Replace("\\", "/");
            string normalizedTail = tail.Replace("\\", "/");

            if (normalizedResolved.Length > normalizedTail.Length)
            {
                return normalizedResolved.Substring(0, normalizedResolved.Length - normalizedTail.Length - 1);
            }

            return normalizedResolved;
        }

        private string CombinePath(string basePath, string tail)
        {
            string normalizedBase = basePath.Replace("\\", "/").TrimEnd('/');
            string normalizedTail = tail.Replace("\\", "/").TrimStart('/');
            return normalizedBase + "/" + normalizedTail;
        }

        // Единая точка добавления строки лога: пишет в накопительный список (возвращается
        // целиком по завершении Import) и одновременно, если UI подписан, отдаёт строку
        // через IProgress<string> сразу же — для построчного вывода по ходу операции.
        private void Report(List<string> log, IProgress<string> progress, string message)
        {
            log.Add(message);

            if (progress != null)
            {
                progress.Report(message);
            }
        }
    }
}
