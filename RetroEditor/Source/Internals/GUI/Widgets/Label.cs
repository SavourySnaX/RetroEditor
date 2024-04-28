
using ImGuiNET;
using RetroEditor.Plugins;

internal class Label : IWidgetLabel, IWidgetUpdateDraw
{
    public string Name { get; set; }

    public Label(string name)
    {
        Name = name;
    }

    public void Update(float seconds)
    {
    }

    public void Draw()
    {
        ImGui.Text(Name);
    }
}
