using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Paths
{
    public interface IPathNormalizer
    {
        BackupItem Normalize(string gameName, string absolutePath);
    }
}
