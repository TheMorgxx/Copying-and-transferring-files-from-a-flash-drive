using System.Collections.Generic;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Games
{
    public interface IGameDefinitionRepository
    {
        List<GameDefinition> GetAll();

        void SaveAll(List<GameDefinition> games);
    }
}
