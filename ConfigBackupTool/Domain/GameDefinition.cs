using System.Collections.Generic;

namespace ConfigBackupTool.Domain
{
    public class GameDefinition
    {
        public string GameName { get; set; }
        public List<PathEntry> Paths { get; set; }
    }
}
