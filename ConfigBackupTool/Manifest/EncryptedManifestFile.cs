namespace ConfigBackupTool.Manifest
{
    // Формат manifest.json на диске: сам файл не зашифрован целиком как блоб,
    // а хранит Salt/IV открытым текстом (они не секрет) и CipherText — уже
    // зашифрованный JSON настоящего BackupManifest.
    internal class EncryptedManifestFile
    {
        public string Salt { get; set; }
        public string IV { get; set; }
        public string CipherText { get; set; }
    }
}
