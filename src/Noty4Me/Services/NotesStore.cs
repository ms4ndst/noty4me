using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Noty4Me.Models;

namespace Noty4Me.Services;

public static class NotesStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static List<Note> Load()
    {
        Paths.EnsureDir();
        if (!File.Exists(Paths.NotesFile)) return new List<Note>();
        try
        {
            var json = File.ReadAllText(Paths.NotesFile);
            return JsonSerializer.Deserialize<List<Note>>(json, Options) ?? new List<Note>();
        }
        catch
        {
            return new List<Note>();
        }
    }

    public static void Save(IEnumerable<Note> notes)
    {
        Paths.EnsureDir();
        File.WriteAllText(Paths.NotesFile, JsonSerializer.Serialize(notes, Options));
    }
}
