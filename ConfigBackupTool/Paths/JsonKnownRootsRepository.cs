using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ConfigBackupTool.Paths
{
    public class JsonKnownRootsRepository : IKnownRootsRepository
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _serializerOptions;

        // По умолчанию файл настроек лежит рядом с exe: known_roots.json
        public JsonKnownRootsRepository()
            : this(Path.Combine(AppContext.BaseDirectory, "known_roots.json"))
        {
        }

        public JsonKnownRootsRepository(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException("filePath");
            }

            _filePath = filePath;
            _serializerOptions = new JsonSerializerOptions();
            _serializerOptions.WriteIndented = true;
        }

        public IReadOnlyList<KnownRoot> GetAll()
        {
            if (File.Exists(_filePath) == false)
            {
                return new List<KnownRoot>();
            }

            string json = File.ReadAllText(_filePath);

            if (string.IsNullOrWhiteSpace(json) == true)
            {
                return new List<KnownRoot>();
            }

            List<KnownRoot> knownRoots;

            try
            {
                knownRoots = JsonSerializer.Deserialize<List<KnownRoot>>(json, _serializerOptions);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Не удалось прочитать файл настроек известных корней: " + _filePath + ". Файл повреждён или имеет неверный формат.", ex);
            }

            if (knownRoots == null)
            {
                return new List<KnownRoot>();
            }

            return knownRoots;
        }

        public void SaveAll(IReadOnlyList<KnownRoot> knownRoots)
        {
            if (knownRoots == null)
            {
                throw new ArgumentNullException("knownRoots");
            }

            List<KnownRoot> toSave = new List<KnownRoot>(knownRoots);
            string json = JsonSerializer.Serialize(toSave, _serializerOptions);

            string directory = Path.GetDirectoryName(_filePath);
            if (string.IsNullOrEmpty(directory) == false && Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_filePath, json);
        }
    }
}
