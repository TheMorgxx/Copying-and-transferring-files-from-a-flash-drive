using System;
using System.Collections.Generic;
using System.IO;
using ConfigBackupTool.Domain;
using ConfigBackupTool.Encryption;
using ConfigBackupTool.Manifest;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.Managers
{
    public class BackupManager : IBackupManager
    {
        private readonly IPathNormalizer _pathNormalizer;
        private readonly IEncryptionService _encryptionService;
        private readonly IBackupManifestRepository _manifestRepository;

        public BackupManager(IPathNormalizer pathNormalizer, IEncryptionService encryptionService, IBackupManifestRepository manifestRepository)
        {
            _pathNormalizer = pathNormalizer;
            _encryptionService = encryptionService;
            _manifestRepository = manifestRepository;
        }

        public List<string> Export(List<GameDefinition> games, string destinationFolder, string password, IProgress<string> progress = null)
        {
            List<string> log = new List<string>();

            Domain.BackupManifest manifest = new Domain.BackupManifest();
            manifest.ManifestVersion = 1;
            manifest.BackupTimestamp = DateTime.UtcNow.ToString("o");
            manifest.Games = new List<GameManifestEntry>();

            for (int i = 0; i < games.Count; i++)
            {
                GameDefinition game = games[i];
                ExportGame(game, destinationFolder, password, manifest, log, progress);
            }

            string manifestPath = Path.Combine(destinationFolder, "manifest.json");
            _manifestRepository.Save(manifest, manifestPath, password);
            Report(log, progress, "manifest.json сохранён");

            return log;
        }

        private void ExportGame(GameDefinition game, string destinationFolder, string password, Domain.BackupManifest manifest, List<string> log, IProgress<string> progress)
        {
            bool isInstalled = false;

            for (int i = 0; i < game.Paths.Count; i++)
            {
                if (DoesPathExist(game.Paths[i].Path) == true)
                {
                    isInstalled = true;
                    break;
                }
            }

            if (isInstalled == false)
            {
                Report(log, progress, game.GameName + ": не установлена");
                return;
            }

            GameManifestEntry gameEntry = new GameManifestEntry();
            gameEntry.GameName = game.GameName;
            gameEntry.Items = new List<ManifestItem>();

            for (int i = 0; i < game.Paths.Count; i++)
            {
                string sourcePath = game.Paths[i].Path;

                if (DoesPathExist(sourcePath) == false)
                {
                    continue;
                }

                ExportPathEntry(game.GameName, sourcePath, destinationFolder, password, gameEntry, log, progress);
            }

            manifest.Games.Add(gameEntry);
            Report(log, progress, game.GameName + ": обработана");
        }

        private void ExportPathEntry(string gameName, string sourcePath, string destinationFolder, string password, GameManifestEntry gameEntry, List<string> log, IProgress<string> progress)
        {
            BackupItem backupItem = _pathNormalizer.Normalize(gameName, sourcePath);

            ManifestItem manifestItem = new ManifestItem();
            manifestItem.EntryType = backupItem.EntryType;
            manifestItem.Anchor = backupItem.Anchor;
            manifestItem.Tail = backupItem.Tail;
            manifestItem.OriginalPath = backupItem.OriginalPath;
            manifestItem.OriginalDrive = backupItem.OriginalDrive;

            if (backupItem.EntryType == BackupEntryType.File)
            {
                try
                {
                    EncryptAndStoreFile(sourcePath, destinationFolder, password, manifestItem);
                }
                catch (Exception ex)
                {
                    Report(log, progress, gameName + ": ошибка при обработке файла " + sourcePath + ": " + ex.Message + " — пропущен");
                    return;
                }
            }
            else
            {
                manifestItem.Files = new List<ManifestFileEntry>();
                ExportDirectory(gameName, sourcePath, destinationFolder, password, manifestItem, log, progress);
            }

            gameEntry.Items.Add(manifestItem);
        }

        private void ExportDirectory(string gameName, string directoryPath, string destinationFolder, string password, ManifestItem manifestItem, List<string> log, IProgress<string> progress)
        {
            string[] filePaths;

            try
            {
                filePaths = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception ex)
            {
                Report(log, progress, gameName + ": ошибка при чтении папки " + directoryPath + ": " + ex.Message + " — папка пропущена");
                return;
            }

            for (int i = 0; i < filePaths.Length; i++)
            {
                string filePath = filePaths[i];

                try
                {
                    ManifestFileEntry fileEntry = EncryptAndStoreDirectoryFile(filePath, directoryPath, destinationFolder, password);
                    manifestItem.Files.Add(fileEntry);
                }
                catch (Exception ex)
                {
                    Report(log, progress, gameName + ": ошибка при обработке файла " + filePath + ": " + ex.Message + " — пропущен");
                }
            }
        }

        private void EncryptAndStoreFile(string sourcePath, string destinationFolder, string password, ManifestItem manifestItem)
        {
            byte[] plainBytes = File.ReadAllBytes(sourcePath);
            EncryptionResult result = _encryptionService.Encrypt(plainBytes, password);

            string encryptedFileName = Guid.NewGuid().ToString("N") + ".enc";
            string destinationPath = Path.Combine(destinationFolder, encryptedFileName);
            File.WriteAllBytes(destinationPath, result.CipherText);

            manifestItem.EncryptedFileName = encryptedFileName;
            manifestItem.Salt = Convert.ToBase64String(result.Salt);
            manifestItem.IV = Convert.ToBase64String(result.IV);
            manifestItem.OriginalHash = result.OriginalHash;
        }

        private ManifestFileEntry EncryptAndStoreDirectoryFile(string filePath, string directoryRoot, string destinationFolder, string password)
        {
            byte[] plainBytes = File.ReadAllBytes(filePath);
            EncryptionResult result = _encryptionService.Encrypt(plainBytes, password);

            string encryptedFileName = Guid.NewGuid().ToString("N") + ".enc";
            string destinationPath = Path.Combine(destinationFolder, encryptedFileName);
            File.WriteAllBytes(destinationPath, result.CipherText);

            string relativePath = Path.GetRelativePath(directoryRoot, filePath).Replace("\\", "/");

            ManifestFileEntry fileEntry = new ManifestFileEntry();
            fileEntry.RelativePath = relativePath;
            fileEntry.EncryptedFileName = encryptedFileName;
            fileEntry.Salt = Convert.ToBase64String(result.Salt);
            fileEntry.IV = Convert.ToBase64String(result.IV);
            fileEntry.OriginalHash = result.OriginalHash;

            return fileEntry;
        }

        private bool DoesPathExist(string path)
        {
            if (File.Exists(path) == true)
            {
                return true;
            }

            if (Directory.Exists(path) == true)
            {
                return true;
            }

            return false;
        }

        // Единая точка добавления строки лога: пишет в накопительный список (возвращается
        // целиком по завершении Export) и одновременно, если UI подписан, отдаёт строку
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
