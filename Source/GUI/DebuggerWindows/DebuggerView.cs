
using System.Numerics;
using ImGuiNET;

internal class DebuggerView : IWindow
{
    public float UpdateInterval => 1.0f/30;

    public DebuggerView(LibMameDebugger debugger, int viewNum, int w, int h)
    {
        this.debugger = debugger;
        view = new LibMameDebugger.DView(viewNum, 0, 0, w, h);
    }

    public void Close()
    {
    }

    public bool Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0,0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
    
        var sizeOfMonoText=ImGui.CalcTextSize("A");
        ImGui.BeginChild("BLAH", new Vector2(sizeOfMonoText.X*(view.w+2), sizeOfMonoText.Y*(view.h+2)),0,0);

        var drawList = ImGui.GetWindowDrawList();
        var convCode = new byte[] { 0, 0 };
        Vector2 pos = ImGui.GetCursorScreenPos();
        for (int yy=0;yy<view.h;yy++)
        {
            for (int xx=0;xx<view.w;xx++)
            {
                var attr=view.state[(yy*view.w+xx)*2+1];
                FetchColourForStyle(attr,out var fg,out var bg);
                convCode[0]=view.state[(yy*view.w+xx)*2];
                drawList.AddRectFilled(pos, new Vector2(pos.X + sizeOfMonoText.X, pos.Y + sizeOfMonoText.Y), ImGui.GetColorU32(bg));
                drawList.AddText(pos, ImGui.GetColorU32(fg), System.Text.Encoding.ASCII.GetString(convCode));
                pos.X += sizeOfMonoText.X;
            }
            pos.X = ImGui.GetCursorScreenPos().X;
            pos.Y += sizeOfMonoText.Y;
        }
        ImGui.EndChild();
        ImGui.PopStyleVar(2);

        return false;
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {
        // Request update todo
        var viewSize = view.w * view.h * 2;
        if (view.state.Length != viewSize)
        {
            view.state = new byte[viewSize];
        }
        debugger.UpdateDView(ref view);
    }

    private LibMameDebugger debugger;
    private LibMameDebugger.DView view;

    private void FetchColourForStyle(byte attr,out Vector4 fg,out Vector4 bg)
    {
        bg = new Vector4(1.0f, 1.0f, 1.0f, .9f);
        fg = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

        if ((attr & 0x01)==0x01)
        {
            fg = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        }
        if ((attr & 0x02)==0x02)
        {
            bg = new Vector4(1.0f, 0.5f, .5f, 0.8f);
        }
        if ((attr & 0x04)==0x04)
        {
            fg = new Vector4(0.0f, 0.0f, 1.0f, 1.0f);
        }
        if ((attr & 0x08)==0x08)
        {
            fg = new Vector4(fg.X * 0.5f, fg.Y * 0.5f, fg.Z * 0.5f, 1.0f);
        }
        if ((attr & 0x10)==0x10)
        {
            bg = new Vector4(0.7f, 0.7f, 0.7f, .9f);
        }
        if ((attr & 0x20)==0x20)
        {
            bg = new Vector4(1.0f, 1.0f, 0.0f, .8f);
        }
        if ((attr & 0x40)==0x40)
        {
            fg = new Vector4(0.0f, .5f, 0.0f, 1.0f);
        }
        if ((attr & 0x80)==0x80)
        {
            bg = new Vector4(0.0f, 1.0f, 1.0f, 0.8f);
        }
    }



}