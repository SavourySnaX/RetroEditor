
using ImGuiNET;
using RetroEditor.Plugins;

internal class SameLine : IWidgetItem, IWidgetUpdateDraw
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