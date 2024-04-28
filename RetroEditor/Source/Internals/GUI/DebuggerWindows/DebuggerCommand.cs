
using System.Numerics;
using ImGuiNET;

internal class DebuggerCommand : IWindow
{
    public float UpdateInterval => 1.0f;
    
    public DebuggerCommand(LibMameDebugger debugger)
    {
        this.debugger = debugger;
        log = "";
        inputBuffer = "";
    }

    public void Close()
    {
    }

    public bool Draw()
    {
        if (ImGui.InputText("Command",ref inputBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            inputBuffer.Trim();
            var result = debugger.SendCommand(inputBuffer);
            if (result.Length > 0)
            {
                log += result + "\n";
            }
        }

        ImGui.BeginChild("Log", new Vector2(0, 0), 0, ImGuiWindowFlags.HorizontalScrollbar|ImGuiWindowFlags.AlwaysVerticalScrollbar);
        ImGui.Text(log);
        ImGui.EndChild();

        return false;
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {
    }

    private LibMameDebugger debugger;
    private string inputBuffer;
    private string log;
}