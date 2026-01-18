using MyMGui;
using RetroEditor.Plugins;

internal class Label : IWidgetLabel, IWidgetUpdateDraw
{
    public string Name { get; set; }

    public Label(string name)
    {
        Name = name;
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }

    public void Draw(IWidgetLog logger)
    {
        ImGui.Text(Name);
    }
}
