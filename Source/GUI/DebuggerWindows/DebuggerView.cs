
using System.Numerics;
using ImGuiNET;

internal class DebuggerView : IWindow
{
    public float UpdateInterval => 1.0f/30;

    string expressionStore = "";

    public DebuggerView(LibMameDebugger debugger, LibRetroPlugin.debug_view_type type, int w, int h, string expression)
    {
        this.debugger = debugger;
        this.view = new LibMameDebugger.DView(debugger.AllocView(type), 0, 0, w, h, expression);
        this.debugger.SetExpression(ref view);
    }

    public void Close()
    {
        debugger.FreeView(view.view);
    }

    public bool Draw()
    {
        float YOff = 0;
        if (this.view.view.Kind == LibRetroPlugin.debug_view_type.Memory || this.view.view.Kind == LibRetroPlugin.debug_view_type.Disassembly)
        {
            // Add Expression
            if (ImGui.InputText("Expression", ref expressionStore, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                view.view.Expression = expressionStore;
                debugger.SetExpression(ref view);
            }
            YOff = ImGui.GetCursorPosY();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0,0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
    
        var sizeOfMonoText=ImGui.CalcTextSize("A");
        ImGui.BeginChild("BLAH", new Vector2(sizeOfMonoText.X*(view.view.W+2), sizeOfMonoText.Y*(view.view.H+2)),0,ImGuiWindowFlags.NoScrollbar);

        var drawList = ImGui.GetWindowDrawList();
        var convCode = new byte[] { 0, 0 };
        Vector2 pos = ImGui.GetCursorScreenPos();
        for (int yy=0;yy<view.view.H;yy++)
        {
            for (int xx=0;xx<view.view.W;xx++)
            {
                var attr=view.state[(yy*view.view.W+xx)*2+1];
                FetchColourForStyle(attr,out var fg,out var bg);
                convCode[0]=view.state[(yy*view.view.W+xx)*2];
                drawList.AddRectFilled(pos, new Vector2(pos.X + sizeOfMonoText.X, pos.Y + sizeOfMonoText.Y), ImGui.GetColorU32(bg));
                drawList.AddText(pos, ImGui.GetColorU32(fg), System.Text.Encoding.ASCII.GetString(convCode));
                pos.X += sizeOfMonoText.X;
            }
            pos.X = ImGui.GetCursorScreenPos().X;
            pos.Y += sizeOfMonoText.Y;
        }
        ImGui.EndChild();
        ImGui.PopStyleVar(2);

        var size = ImGui.GetWindowSize();
        size.Y -= YOff;
        var expectedSize = (int)Math.Floor(size.Y / sizeOfMonoText.Y)-2;
        if (view.view.H != expectedSize)
        {
            view.view.H = expectedSize;
            view.state = new byte[view.view.W * view.view.H * 2];
        }

        if (ImGui.IsWindowFocused())
        {
            if (this.view.view.Kind == LibRetroPlugin.debug_view_type.Memory || this.view.view.Kind == LibRetroPlugin.debug_view_type.Disassembly)
            {
                if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_PDOWN);
                    }
                    else
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_DOWN);
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_PUP);
                    }
                    else
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_UP);
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow))
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_CTRLLEFT);
                    }
                    else
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_LEFT);
                    }
                }
                if (ImGui.IsKeyPressed(ImGuiKey.RightArrow))
                {
                    if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_CTRLRIGHT);
                    }
                    else
                    {
                        debugger.ProcessKey(ref view, LibRetroPlugin.debug_key.DCH_RIGHT);
                    }
                }
            }
        }

        return false;
    }

    public bool Initialise()
    {
        return true;
    }

    public void Update(float seconds)
    {
        // Request update todo
        var viewSize = view.view.W * view.view.H * 2;
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