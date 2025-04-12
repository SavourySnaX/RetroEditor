
using System.Numerics;
using ImGuiNET;

internal class DebuggerCommand : IWindow
{
    public float UpdateInterval => 1.0f;
    
    public DebuggerCommand(LibMameDebugger debugger)
    {
        this.debugger = debugger;
        log = new List<string>();
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
                var split = result.Split('\n');
                log.AddRange(split);
            }
        }


        if (ImGui.BeginChild("Scrolling", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            unsafe
            {
                var clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
                ImGuiNative.ImGuiListClipper_Begin(clipper, log.Count, -1.0f);
                while (ImGuiNative.ImGuiListClipper_Step(clipper) != 0)
                {
                    for (int i = clipper->DisplayStart; i < clipper->DisplayEnd; i++)
                    {
                        ImGui.TextUnformatted(log[i]);
                    }
                }
                ImGuiNative.ImGuiListClipper_End(clipper);
                ImGuiNative.ImGuiListClipper_destroy(clipper);
            }
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
            {
                ImGui.SetScrollHereY(1.0f);
            }
        }
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
    private List<string> log;
}