
using ImGuiNET;

public class Slider : IWidgetRanged, IWidgetUpdateDraw
{
    public Slider(string label, int value, int min, int max, ChangedEventHandler changed)
    {
        _label = label;
        _value = value;
        _min = min;
        _max = max;
        Handler = changed;
        Enabled = true;
    }

    private string _label;
    private int _value;
    private int _min;
    private int _max;

    public ChangedEventHandler? Handler { get; set; }
    public int Value { get => _value; set => _value = value; }
    public bool Enabled { get; set; }

    public void Update(float seconds)
    {
    }

    public void Draw()
    {
        if (!Enabled)
        {
            ImGui.BeginDisabled();
        }
        var changed = ImGui.SliderInt(_label, ref _value, _min, _max);
        if (!Enabled)
        {
            ImGui.EndDisabled();
        }
        if (changed)
        {
            Handler?.Invoke();
        }
    }
}