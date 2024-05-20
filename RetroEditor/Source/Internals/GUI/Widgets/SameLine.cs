
using ImGuiNET;
using RetroEditor.Plugins;

internal class SameLine : IWidgetItem, IWidgetUpdateDraw
{
    public SameLine()
    {
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }

    public void Draw(IWidgetLog logger)
    {
        ImGui.SameLine();
    }
}