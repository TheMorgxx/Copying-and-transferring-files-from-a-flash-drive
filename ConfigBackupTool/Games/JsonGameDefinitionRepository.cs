using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Games
{
    public class JsonGameDefinitionRepository : IGameDefinitionRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _serializerOptions;

        // По умолчанию файл списка игр лежит рядом с exe: games.json
        public JsonGameDefinitionRepository()
            : this(Path.Combine(AppContext.BaseDirectory, "games.json"))
        {
        }

        public JsonGameDefinitionRepository(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            _filePath = filePath;
            _serializerOptions = new JsonSerializerOptions();
            _serializerOptions.WriteIndented = true;
        }

        public List<GameDefinition> GetAll()
        {
            if (File.Exists(_filePath) == false)
            {
                return new List<GameDefinition>();
            }

            string json = File.ReadAllText(_filePath);

            if (string.IsNullOrWhiteSpace(json) == true)
            {
                return new List<GameDefinition>();
            }

            List<GameDefinition> games;

            try
            {
                games = JsonSerializer.Deserialize<List<GameDefinition>>(json, _serializerOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Не удалось прочитать файл списка игр: " + _filePath + ". Файл повреждён или имеет неверный формат.", ex);
            }

            if (games == null)
            {
                return new List<GameDefinition>();
            }

            NormalizeLoadedGames(games);

            return games;
        }

        public void SaveAll(List<GameDefinition> games)
        {
            if (games == null)
            {
                throw new ArgumentNullException("games");
            }

            string json = JsonSerializer.Serialize(games, _serializerOptions);

            string directory = Path.GetDirectoryName(_filePath);
            if (string.IsNullOrEmpty(directory) == false && Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, json);
        }

        // Деформированный games.json (например, отредактированный вручную) не должен ронять
        // приложение при загрузке — подчищаем null-коллекции до пустых списков, чтобы
        // ExportForm мог сразу работать со списком без дополнительных проверок на каждый элемент.
        private void NormalizeLoadedGames(List<GameDefinition> games)
        {
            for (int i = 0; i < games.Count; i++)
            {
                if (games[i].Paths == null)
                {
                    games[i].Paths = new List<PathEntry>();
                }
            }
        }
    }
}
