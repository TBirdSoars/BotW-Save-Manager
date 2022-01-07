using System.IO;

namespace BotWSaveManager.UI.Logging
{
    public static class LogHelper
    {
        public static string VisualizeDirectoryAsString(string path, int depth = 0)
        {
            string result = path;

            foreach (string file in Directory.EnumerateFiles(path))
            {
                result += $"\n{new string('\t', depth + 1)}{file}";
            }

            foreach (string directory in Directory.EnumerateDirectories(path))
            {
                result += $"\n{new string('\t', depth + 1)}{VisualizeDirectoryAsString(directory, depth + 1)}";
            }

            return result;
        }
    }
}
