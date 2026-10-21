namespace ConfigBackupTool.Domain
{
    public enum AnchorType
    {
        UserProfile,
        AppDataRoaming,
        DriveRelative
    }

    public enum BackupEntryType
    {
        File,
        Directory
    }

    public class BackupItem
    {
        public string GameName { get; set; }
        public AnchorType Anchor { get; set; }
        public string Tail { get; set; }
        public string OriginalPath { get; set; }
        public string OriginalDrive { get; set; }
        public BackupEntryType EntryType { get; set; }
    }
}
