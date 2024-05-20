
using ImGuiNET;
using RetroEditor.Plugins;

internal class Seperator : IWidgetItem, IWidgetUpdateDraw
{
    public Seperator()
    {
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }

    public void Draw(IWidgetLog logger)
    {
        ImGui.Separator();
    }
}