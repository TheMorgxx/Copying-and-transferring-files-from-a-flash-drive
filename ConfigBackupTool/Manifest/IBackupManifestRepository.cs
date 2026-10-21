using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Manifest
{
    public interface IBackupManifestRepository
    {
        void Save(BackupManifest manifest, string filePath, string password);

        BackupManifest Load(string filePath, string password);
    }
}
