
using ImGuiNET;

public class SameLine : IWidgetItem, IWidgetUpdateDraw
{
    public SameLine()
    {
    }

    public void Update(float seconds)
    {
    }

    public void Draw()
    {
        ImGui.SameLine();
    }
}