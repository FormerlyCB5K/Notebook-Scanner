using System.Text.Json;

namespace NotebookScanner;

public static class DeviceMemory
{
    public static string? Read(string path)
    {
        try { return JsonSerializer.Deserialize<string>(File.ReadAllText(path)); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return null; }
    }

    public static void Write(string path, string id)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(id));
        File.Move(temporary, path, true);
    }
}
