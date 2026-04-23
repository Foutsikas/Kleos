using Godot;
using Godot.Collections;

public static class ResourceScanner
{
    // Loads all Resource files of type T from a directory.
    // Handles both .tres (editor) and .res (exported builds).
    // Returns an untyped Array for compatibility with Godot exports.

    public static Array LoadAll<T>(string directoryPath) where T : Resource
    {
        var results = new Array();
        var dir = DirAccess.Open(directoryPath);
        if (dir == null)
        {
            GD.PrintErr($"[ResourceScanner] Could not open directory: {directoryPath}");
            return results;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (!dir.CurrentIsDir()
                && (fileName.EndsWith(".tres") || fileName.EndsWith(".res")))
            {
                string fullPath = $"{directoryPath}{fileName}";
                var resource = GD.Load(fullPath);
                if (resource is T typed)
                    results.Add(typed);
            }
            fileName = dir.GetNext();
        }
        dir.ListDirEnd();

        return results;
    }
}