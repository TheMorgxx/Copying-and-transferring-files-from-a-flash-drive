using System;
using System.Collections.Generic;
using System.IO;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Paths
{
    public class PathResolver : IPathResolver
    {
        private readonly IKnownRootsRepository _knownRootsRepository;

        // knownRootsRepository может быть null, если известные корни не настроены -
        // тогда этот шаг просто пропускается
        public PathResolver(IKnownRootsRepository knownRootsRepository)
        {
            _knownRootsRepository = knownRootsRepository;
        }

        public string Resolve(BackupItem item, Func<IReadOnlyList<string>, string> ambiguityResolver)
        {
            if (item == null)
            {
                return null;
            }

            if (item.Anchor == AnchorType.UserProfile)
            {
                string candidate = CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), item.Tail);
                if (PathExists(candidate, item.EntryType) == true)
                {
                    return candidate;
                }

                return null;
            }

            if (item.Anchor == AnchorType.AppDataRoaming)
            {
                string candidate = CombinePath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), item.Tail);
                if (PathExists(candidate, item.EntryType) == true)
                {
                    return candidate;
                }

                return null;
            }

            return ResolveDriveRelative(item, ambiguityResolver);
        }

        private string ResolveDriveRelative(BackupItem item, Func<IReadOnlyList<string>, string> ambiguityResolver)
        {
            // Шаг 0: известные корни (Steam/Epic и т.д.), заданные пользователем в настройках
            List<string> knownRootCandidates = FindKnownRootCandidates(item);
            if (knownRootCandidates.Count == 1)
            {
                return knownRootCandidates[0];
            }

            if (knownRootCandidates.Count > 1)
            {
                if (ambiguityResolver == null)
                {
                    return null;
                }

                return ambiguityResolver(knownRootCandidates);
            }

            // Быстрый путь: структура дисков не изменилась с момента бэкапа
            if (item.OriginalPath != null && PathExists(item.OriginalPath, item.EntryType) == true)
            {
                return item.OriginalPath;
            }

            List<string> candidates = new List<string>();
            DriveInfo[] drives = DriveInfo.GetDrives();

            for (int i = 0; i < drives.Length; i++)
            {
                DriveInfo drive = drives[i];

                if (drive.IsReady == false)
                {
                    continue;
                }

                string driveLetter = drive.Name.TrimEnd('\\').TrimEnd('/');

                if (item.OriginalDrive != null && string.Equals(driveLetter, item.OriginalDrive, StringComparison.OrdinalIgnoreCase) == true)
                {
                    continue;
                }

                string directCandidate = CombinePath(driveLetter, item.Tail);
                if (PathExists(directCandidate, item.EntryType) == true)
                {
                    candidates.Add(directCandidate);
                }

                AddSubfolderCandidates(drive, item, candidates);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            if (candidates.Count == 1)
            {
                return candidates[0];
            }

            if (ambiguityResolver == null)
            {
                return null;
            }

            return ambiguityResolver(candidates);
        }

        private List<string> FindKnownRootCandidates(BackupItem item)
        {
            List<string> found = new List<string>();

            if (_knownRootsRepository == null)
            {
                return found;
            }

            IReadOnlyList<KnownRoot> knownRoots = _knownRootsRepository.GetAll();
            if (knownRoots == null)
            {
                return found;
            }

            for (int i = 0; i < knownRoots.Count; i++)
            {
                KnownRoot knownRoot = knownRoots[i];

                if (knownRoot.RootPath == null || knownRoot.Marker == null)
                {
                    continue;
                }

                bool tailMatchesMarker = item.Tail.StartsWith(knownRoot.Marker, StringComparison.OrdinalIgnoreCase);
                if (tailMatchesMarker == false)
                {
                    continue;
                }

                string candidate = CombinePath(knownRoot.RootPath, item.Tail);
                if (PathExists(candidate, item.EntryType) == true)
                {
                    found.Add(candidate);
                }
            }

            return found;
        }

        private void AddSubfolderCandidates(DriveInfo drive, BackupItem item, List<string> candidates)
        {
            string[] subFolders;

            try
            {
                subFolders = Directory.GetDirectories(drive.RootDirectory.FullName);
            }
            catch (Exception)
            {
                return;
            }

            for (int i = 0; i < subFolders.Length; i++)
            {
                string candidate = CombinePath(subFolders[i], item.Tail);
                if (PathExists(candidate, item.EntryType) == true)
                {
                    candidates.Add(candidate);
                }
            }
        }

        private bool PathExists(string path, BackupEntryType entryType)
        {
            if (entryType == BackupEntryType.Directory)
            {
                return Directory.Exists(path);
            }

            return File.Exists(path);
        }

        private string CombinePath(string basePath, string tail)
        {
            string normalizedBase = basePath.Replace("\\", "/").TrimEnd('/');
            string normalizedTail = tail.Replace("\\", "/").TrimStart('/');
            return normalizedBase + "/" + normalizedTail;
        }
    }
}
