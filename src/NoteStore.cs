using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

/// <summary>Reads and atomically replaces the existing notes.json format.</summary>
sealed class NoteStore
{
    readonly string filePath;
    public NoteStore(string directory)
    {
        Directory.CreateDirectory(directory);
        filePath = Path.Combine(directory, "notes.json");
    }

    public bool Exists
    {
        get
        {
            return File.Exists(filePath);
        }
    }

    public List<NoteData> Load()
    {
        using (var stream = File.OpenRead(filePath))
            return (List<NoteData>)CreateSerializer().ReadObject(stream);
    }

    public void Save(List<NoteData> notes)
    {
        string temporaryPath = filePath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            CreateSerializer().WriteObject(stream, notes);
            stream.Flush(true);
        }

        Native.CommitFile(temporaryPath, filePath);
    }

    static DataContractJsonSerializer CreateSerializer()
    {
        return new DataContractJsonSerializer(typeof(List<NoteData>));
    }
}
