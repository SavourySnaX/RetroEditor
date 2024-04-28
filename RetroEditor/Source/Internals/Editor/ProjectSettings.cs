using System.Text.Json;

internal sealed class ProjectSettings
{
    public sealed class SerializedSettings
    {
        public SerializedSettings(string version, string retroCoreName, string retroPluginName, string originalRomName)
        {
            Version = version;
            RetroCoreName = retroCoreName;
            RetroPluginName = retroPluginName;
            OriginalRomName = originalRomName;
        }
        public string Version { get; private set; }
        public string RetroCoreName { get; private set; }
        public string RetroPluginName { get; private set; }
        public string OriginalRomName { get; private set; }
    }

    public ProjectSettings(string projectName, string projectPath, string retroCoreName, string retroPluginName, string originalRomName)
    {
        this.serializedSettings = new SerializedSettings(Editor.EditorSettings.CurrentVersion, retroCoreName, retroPluginName, originalRomName);
        this.projectPath = projectPath;
        this.projectName = projectName;
    }

    internal void Save(string projectFile)
    {
        var json = JsonSerializer.Serialize(serializedSettings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(projectFile, json);

    }
    internal void Load(string projectFile)
    {
        var temp = JsonSerializer.Deserialize<SerializedSettings>(File.ReadAllText(projectFile));
        if (temp != null)
        {
            if (temp.Version != serializedSettings.Version)
            {
                // TODO - upgrade
            }
            serializedSettings = temp;
        }
    }

    internal SerializedSettings serializedSettings;

    public string Version => serializedSettings.Version;
    public string RetroCoreName => serializedSettings.RetroCoreName;
    public string RetroPluginName => serializedSettings.RetroPluginName;
    public string OriginalRomName => serializedSettings.OriginalRomName;
    internal readonly string projectName;
    internal readonly string projectPath;
}

