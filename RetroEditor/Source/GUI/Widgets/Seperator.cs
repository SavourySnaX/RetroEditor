
using ImGuiNET;

public class Seperator : IWidgetItem, IWidgetUpdateDraw
{
    public Seperator()
    {
    }

    public void Update(float seconds)
    {
    }

    public void Draw()
    {
        ImGui.Separator();
    }
}