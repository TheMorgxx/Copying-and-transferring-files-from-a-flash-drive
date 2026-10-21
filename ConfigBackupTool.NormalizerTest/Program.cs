using System;
using System.Collections.Generic;
using System.IO;
using ConfigBackupTool.Domain;
using ConfigBackupTool.Paths;

namespace ConfigBackupTool.NormalizerTest
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string listFilePath = args.Length > 0 ? args[0] : "backup_list.txt";

            if (File.Exists(listFilePath) == false)
            {
                Console.WriteLine("Файл не найден: " + listFilePath);
                Console.WriteLine("Запустите так: dotnet run -- путь\\к\\backup_list.txt");
                return;
            }

            Dictionary<string, List<string>> templates = ParseBackupList(listFilePath);
            IPathNormalizer normalizer = new PathNormalizer();

            foreach (KeyValuePair<string, List<string>> game in templates)
            {
                Console.WriteLine();
                Console.WriteLine("=== [" + game.Key + "] ===");

                foreach (string rawPath in game.Value)
                {
                    BackupItem item = normalizer.Normalize(game.Key, rawPath);

                    Console.WriteLine("  original: " + item.OriginalPath);
                    Console.WriteLine("    anchor: " + item.Anchor + "  drive: " + (item.OriginalDrive ?? "-") + "  type: " + item.EntryType);
                    Console.WriteLine("    tail:   " + item.Tail);
                }
            }
        }

        private static Dictionary<string, List<string>> ParseBackupList(string path)
        {
            Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
            string currentGame = null;

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith("#") == true)
                {
                    continue;
                }

                if (line.StartsWith("[") == true && line.EndsWith("]") == true)
                {
                    currentGame = line.Substring(1, line.Length - 2);
                    data[currentGame] = new List<string>();
                    continue;
                }

                if (currentGame != null)
                {
                    data[currentGame].Add(line.Replace("\\", "/"));
                }
            }

            return data;
        }
    }
}
