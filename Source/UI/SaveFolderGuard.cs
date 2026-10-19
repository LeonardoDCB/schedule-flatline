using System;
using System.Collections.Generic;
using System.IO;

namespace Flatline
{
    internal static class SaveFolderGuard
    {
        // Fail closed unless this is exactly Saves/<profile>/SaveGame_<active slot>.
        internal static bool TryValidate(string savesRoot, string savePath, int slotNumber, out string fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(savesRoot) || string.IsNullOrWhiteSpace(savePath) ||
                slotNumber < 0 || !Path.IsPathRooted(savePath)) return false;

            try
            {
                string root = Path.GetFullPath(savesRoot);
                string candidate = Path.GetFullPath(savePath);
                DirectoryInfo save = new(candidate);
                if (!save.Exists || save.Parent?.Parent == null) return false;
                if (!string.Equals(Path.GetFullPath(save.Parent.Parent.FullName), root, StringComparison.OrdinalIgnoreCase)) return false;
                if (!string.Equals(save.Name, $"SaveGame_{slotNumber}", StringComparison.OrdinalIgnoreCase)) return false;
                if ((new DirectoryInfo(root).Attributes & FileAttributes.ReparsePoint) != 0 ||
                    (save.Attributes & FileAttributes.ReparsePoint) != 0 ||
                    (save.Parent.Attributes & FileAttributes.ReparsePoint) != 0) return false;

                Stack<DirectoryInfo> pending = new();
                pending.Push(save);
                while (pending.Count > 0)
                {
                    foreach (DirectoryInfo child in pending.Pop().EnumerateDirectories())
                    {
                        if ((child.Attributes & FileAttributes.ReparsePoint) != 0) return false;
                        pending.Push(child);
                    }
                }

                fullPath = candidate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
