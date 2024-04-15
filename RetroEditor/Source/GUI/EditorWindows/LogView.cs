using System.ComponentModel;
using ImGuiNET;

class LogView : IWindow
{
    public float UpdateInterval => 0.5f;

    private Editor editor;

    private string[] cachedLog = Array.Empty<string>();
    private Dictionary<string, string[]> cachedLogPerSource = new Dictionary<string, string[]>();

    public LogView(Editor editor)
    {
        this.editor = editor;
    }

    public void Close()
    {
        // User cancelled
    }

    private void DrawLog(string[] log)
    {
        if (ImGui.BeginChild("Scrolling", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
        {
            unsafe
            {
                var clipper = ImGuiNative.ImGuiListClipper_ImGuiListClipper();
                ImGuiNative.ImGuiListClipper_Begin(clipper, log.Length, -1.0f);
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

            ImGui.EndChild();
        }
    }

    public bool Draw()
    {
        // Start just showing the log
        if (ImGui.BeginTabBar("LogTabs"))
        {
            if (ImGui.BeginTabItem("All"))
            {
                DrawLog(cachedLog);
                ImGui.EndTabItem();
            }
            foreach (var source in cachedLogPerSource.Keys)
            {
                if (ImGui.BeginTabItem(source))
                {
                    DrawLog(cachedLogPerSource[source]);
                    ImGui.EndTabItem();
                }
            }
            ImGui.EndTabBar();
        }
        return false;
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {
        if (Editor.AccessLog == null)
        {
            return;
        }
        var count = Editor.AccessLog.Count();
        if (cachedLog.Length != count)
        {
            var index = 0;
            cachedLog = new string[count];
            while (index < count)
            {
                cachedLog[index] = Editor.AccessLog.Entry(index).ToString();
                index++;
            }
        }
        // Cache seperate log areas too
        foreach (var source in Editor.AccessLog.Sources())
        {
            var sourceCount = Editor.AccessLog.Count(source);
            if (!cachedLogPerSource.ContainsKey(source) || cachedLogPerSource[source].Length != sourceCount)
            {
                var index = 0;
                var sourceLog = new string[sourceCount];
                while (index < sourceCount)
                {
                    sourceLog[index] = Editor.AccessLog.Entry(source, index).ToString();
                    index++;
                }
                cachedLogPerSource[source] = sourceLog;
            }
        }

    }

}
