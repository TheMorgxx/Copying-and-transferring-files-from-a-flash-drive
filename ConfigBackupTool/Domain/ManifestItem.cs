using System.Collections.Generic;

namespace ConfigBackupTool.Domain
{
    public class ManifestItem
    {
        public BackupEntryType EntryType { get; set; }
        public AnchorType Anchor { get; set; }
        public string Tail { get; set; }
        public string OriginalPath { get; set; }
        public string OriginalDrive { get; set; }

        // Заполняется только когда EntryType == File.
        // Имя на флешке обезличено (GUID), соответствие оригиналу есть только здесь, в манифесте.
        public string EncryptedFileName { get; set; }
        public string Salt { get; set; }
        public string IV { get; set; }
        public string OriginalHash { get; set; }

        // Заполняется только когда EntryType == Directory.
        // Каждый файл внутри папки шифруется и диффится независимо от остальных.
        public List<ManifestFileEntry> Files { get; set; }
    }

    public class ManifestFileEntry
    {
        // Путь файла относительно самой папки (Tail владельца), например "skins/custom/blue.png"
        public string RelativePath { get; set; }
        public string EncryptedFileName { get; set; }
        public string Salt { get; set; }
        public string IV { get; set; }
        public string OriginalHash { get; set; }
    }
}
