using System.Collections.Generic;

namespace ConfigBackupTool.Domain
{
    public class BackupManifest
    {
        public int ManifestVersion { get; set; }
        public string BackupTimestamp { get; set; }
        public List<GameManifestEntry> Games { get; set; }
    }

    public class GameManifestEntry
    {
        public string GameName { get; set; }
        public List<ManifestItem> Items { get; set; }
    }
}
