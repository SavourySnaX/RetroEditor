
using RetroEditor.Plugins;

internal interface IEditorInternal
{
    public byte[] LoadState(ProjectSettings settings);
    public void SaveState(byte[] state, ProjectSettings settings);
    public string GetRomPath(ProjectSettings settings);
    public string GetEditorDataPath(ProjectSettings settings, string name);

    public void OpenWindow(IWindow window, string name);
    public void CloseWindow(string name);

    public Log AccessLog { get; }
    public void Log(LogType type, string logSource, string message);
}

