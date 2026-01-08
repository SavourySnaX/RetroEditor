using MyMGui;
using RetroEditor.Plugins;

internal class Checkbox : IWidgetCheckable, IWidgetUpdateDraw
{
    public Checkbox(string label, bool value, ChangedEventHandler changed)
    {
        _label = label;
        _value = value;
        Handler = changed;
        Enabled= true;
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }

    public void Draw(IWidgetLog logger)
    {
        if (!Enabled)
        {
            ImGui.BeginDisabled();
        }
        bool result = ImGui.Checkbox(_label, ref _value);
        if (!Enabled)
        {
            ImGui.EndDisabled();
        }
        if (result)
        {
            Handler?.Invoke();
        }
    }

    private string _label;
    private bool _value;

    public ChangedEventHandler? Handler { get; set; }
    public bool Enabled { get; set; }
    public bool Checked { get => _value; set => _value = value; }
}