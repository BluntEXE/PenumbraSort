using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PenumbraSort.Session;

public static class SessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static void Save(string filePath, IReadOnlyDictionary<string, string?> proposal)
    {
        var json = JsonSerializer.Serialize(proposal, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static Dictionary<string, string?> Load(string filePath)
    {
        if (!File.Exists(filePath))
            return new Dictionary<string, string?>();

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<Dictionary<string, string?>>(json, JsonOptions)
            ?? new Dictionary<string, string?>();
    }
}
