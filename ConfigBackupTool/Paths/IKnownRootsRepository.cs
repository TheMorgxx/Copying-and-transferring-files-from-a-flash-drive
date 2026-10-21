using System.Collections.Generic;

namespace ConfigBackupTool.Paths
{
    public class KnownRoot
    {
        // Маркер должен совпадать с одним из _stableMarkers в PathNormalizer,
        // например "steamapps/common/"
        public string Marker { get; set; }

        // Корневая папка на этой машине, например "D:/SteamLibrary"
        public string RootPath { get; set; }
    }

    public interface IKnownRootsRepository
    {
        IReadOnlyList<KnownRoot> GetAll();

        void SaveAll(IReadOnlyList<KnownRoot> knownRoots);
    }
}
