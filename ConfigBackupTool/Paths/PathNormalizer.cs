using System;
using System.Collections.Generic;
using System.IO;
using ConfigBackupTool.Domain;

namespace ConfigBackupTool.Paths
{
    public class PathNormalizer : IPathNormalizer
    {
        private readonly List<string> _stableMarkers;

        public PathNormalizer()
        {
            _stableMarkers = new List<string>();
            _stableMarkers.Add("steamapps/common/");
        }

        public BackupItem Normalize(string gameName, string absolutePath)
        {
            if (absolutePath == null)
            {
                throw new ArgumentNullException("absolutePath");
            }

            string normalizedPath = absolutePath.Replace("\\", "/");
            string trimmedPath = normalizedPath.TrimEnd('/');
            BackupEntryType entryType = DetermineEntryType(trimmedPath);

            int appDataIndex = IndexOfIgnoreCase(normalizedPath, "AppData/Roaming/");
            if (appDataIndex >= 0)
            {
                string tail = normalizedPath.Substring(appDataIndex + "AppData/Roaming/".Length);
                return BuildItem(gameName, AnchorType.AppDataRoaming, tail, normalizedPath, null, entryType);
            }

            int usersIndex = IndexOfIgnoreCase(normalizedPath, "/Users/");
            if (usersIndex >= 0)
            {
                string afterUsers = normalizedPath.Substring(usersIndex + "/Users/".Length);
                int nextSlash = afterUsers.IndexOf('/');
                if (nextSlash >= 0)
                {
                    string tail = afterUsers.Substring(nextSlash + 1);
                    return BuildItem(gameName, AnchorType.UserProfile, tail, normalizedPath, null, entryType);
                }
            }

            string driveLetter = ExtractDriveLetter(normalizedPath);
            string driveTail = ExtractDriveTail(normalizedPath, driveLetter);
            string finalTail = ApplyStableMarker(driveTail);

            return BuildItem(gameName, AnchorType.DriveRelative, finalTail, normalizedPath, driveLetter, entryType);
        }

        private BackupItem BuildItem(string gameName, AnchorType anchor, string tail, string originalPath, string originalDrive, BackupEntryType entryType)
        {
            BackupItem item = new BackupItem();
            item.GameName = gameName;
            item.Anchor = anchor;
            item.Tail = tail;
            item.OriginalPath = originalPath;
            item.OriginalDrive = originalDrive;
            item.EntryType = entryType;
            return item;
        }

        private BackupEntryType DetermineEntryType(string path)
        {
            if (Directory.Exists(path) == true)
            {
                return BackupEntryType.Directory;
            }

            return BackupEntryType.File;
        }

        private int IndexOfIgnoreCase(string source, string value)
        {
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase);
        }

        private string ExtractDriveLetter(string normalizedPath)
        {
            if (normalizedPath.Length >= 2 && normalizedPath[1] == ':')
            {
                return normalizedPath.Substring(0, 2);
            }

            return null;
        }

        private string ExtractDriveTail(string normalizedPath, string driveLetter)
        {
            if (driveLetter == null)
            {
                return normalizedPath;
            }

            string rest = normalizedPath.Substring(driveLetter.Length);
            if (rest.StartsWith("/") == true)
            {
                rest = rest.Substring(1);
            }

            return rest;
        }

        private string ApplyStableMarker(string driveTail)
        {
            int bestIndex = -1;

            for (int i = 0; i < _stableMarkers.Count; i++)
            {
                string marker = _stableMarkers[i];
                int idx = IndexOfIgnoreCase(driveTail, marker);
                if (idx >= 0 && (bestIndex == -1 || idx < bestIndex))
                {
                    bestIndex = idx;
                }
            }

            if (bestIndex >= 0)
            {
                return driveTail.Substring(bestIndex);
            }

            return driveTail;
        }
    }
}
