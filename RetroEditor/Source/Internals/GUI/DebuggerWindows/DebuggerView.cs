
using System.Numerics;
using ImGuiNET;
using RetroEditor.Source.Internals.GUI;

internal class DebuggerView : IWindow
{
    public float UpdateInterval => 1.0f/30;
    public bool MinimumSize => false;

    int sourceIndex =0;
    string expressionStore = "";
    LibRetroPlugin.debug_format displayFormat = LibRetroPlugin.debug_format.DataFormat1ByteHex;
    LibRetroPlugin.debug_format addressFormat = LibRetroPlugin.debug_format.HexAddress;
    LibRetroPlugin.debug_format typeFormat = LibRetroPlugin.debug_format.LogicalAddress;
    LibRetroPlugin.debug_format rightColumn = LibRetroPlugin.debug_format.AsmRightColumnRawOpcodes;
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
        if (debugger.GetSourcesCount(ref view) > 1)
        {
            if (ImGui.Combo("Source", ref sourceIndex, debugger.GetSourcesList(ref view), debugger.GetSourcesCount(ref view)))
            {
                debugger.SetSource(ref view, sourceIndex);
            }
        }
        if (this.view.view.Kind == LibRetroPlugin.debug_view_type.Memory || this.view.view.Kind == LibRetroPlugin.debug_view_type.Disassembly)
        {
            // Add Expression
            if (ImGui.InputText("Expression", ref expressionStore, 64, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                view.view.Expression = expressionStore;
                debugger.SetExpression(ref view);
            }
            if (this.view.view.Kind == LibRetroPlugin.debug_view_type.Memory)
            {
                var format = (int)displayFormat - (int)LibRetroPlugin.debug_format.DataFormat1ByteHex;
                if (ImGui.Combo("Format", ref format, new string[] 
                    { 
                        "1 Byte Hex", 
                        "2 Byte Hex", 
                        "4 Byte Hex", 
                        "8 Byte Hex",
                        "1 Byte Octal", 
                        "2 Byte Octal", 
                        "4 Byte Octal", 
                        "8 Byte Octal",
                        "32 Bit Float",
                        "64 Bit Float",
                        "80 Bit Float",
                    }, 11))
                {
                    displayFormat = (LibRetroPlugin.debug_format)((format + LibRetroPlugin.debug_format.DataFormat1ByteHex));
                    debugger.SetDataFormat(ref view, displayFormat);
                }
                var address = (int)addressFormat - (int)LibRetroPlugin.debug_format.HexAddress;
                if (ImGui.Combo("Address Format", ref address, new string[] 
                    { 
                        "Hexadecimal", 
                        "Decimal", 
                        "Octal"
                    }, 3))
                {
                    addressFormat = (LibRetroPlugin.debug_format)((address + LibRetroPlugin.debug_format.HexAddress));
                    debugger.SetDataFormat(ref view, addressFormat);
                }
                var type = (int)typeFormat - (int)LibRetroPlugin.debug_format.LogicalAddress;
                if (ImGui.Combo("Type Format", ref type, new string[] 
                    { 
                        "Logical Address", 
                        "Physical Address"
                    }, 2))
                {
                    typeFormat = (LibRetroPlugin.debug_format)((type + LibRetroPlugin.debug_format.LogicalAddress));
                    debugger.SetDataFormat(ref view, typeFormat);
                }
            }
            else
            {
                var right = (int)rightColumn - (int)LibRetroPlugin.debug_format.AsmRightColumnNone;
                if (ImGui.Combo("Right Column", ref right, new string[] 
                    { 
                        "None",
                        "Raw Opcodes", 
                        "Encrypted Opcodes",
                        "Comments"
                    }, 4))
                {
                    rightColumn = (LibRetroPlugin.debug_format)((right + LibRetroPlugin.debug_format.AsmRightColumnNone));
                    debugger.SetDataFormat(ref view, rightColumn);
                }
            }
            YOff = ImGui.GetCursorPosY();
        }

        AbiSafe_ImGuiWrapper.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0,0));
        AbiSafe_ImGuiWrapper.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
    
        var sizeOfMonoText=ImGui.CalcTextSize("A");
        AbiSafe_ImGuiWrapper.BeginChild("BLAH", new Vector2(sizeOfMonoText.X*(view.view.W+2), sizeOfMonoText.Y*(view.view.H+2)),0,ImGuiWindowFlags.NoScrollbar);

        var convCode = new byte[] { 0, 0 };
        Vector2 pos = ImGui.GetCursorScreenPos();
        var initialX = pos.X;
        for (int yy=0;yy<view.view.H;yy++)
        {
            AbiSafe_ImGuiWrapper.BeginChild($"Line{yy}", new Vector2(sizeOfMonoText.X*view.view.W, sizeOfMonoText.Y), 0, ImGuiWindowFlags.NoScrollbar);
            var drawList = ImGui.GetWindowDrawList();
            for (int xx=0;xx<view.view.W;xx++)
            {
                var attr=view.state[(yy*view.view.W+xx)*2+1];
                FetchColourForStyle(attr,out var fg,out var bg);
                convCode[0]=view.state[(yy*view.view.W+xx)*2];
                AbiSafe_ImGuiWrapper.DrawList_AddRectFilled(drawList, pos, new Vector2(pos.X + sizeOfMonoText.X, pos.Y + sizeOfMonoText.Y), AbiSafe_ImGuiWrapper.GetColorU32(bg));
                AbiSafe_ImGuiWrapper.DrawList_AddText(drawList, pos, AbiSafe_ImGuiWrapper.GetColorU32(fg), System.Text.Encoding.ASCII.GetString(convCode));
                pos.X += sizeOfMonoText.X;
            }
            ImGui.EndChild();
            pos.X = initialX;
            pos.Y += sizeOfMonoText.Y;
        }
        
        if (ImGui.IsWindowFocused() || ImGui.IsItemActivated())
        {
            if (this.view.view.Kind == LibRetroPlugin.debug_view_type.Disassembly)
            {
                if (ImGui.IsKeyPressed(ImGuiKey.F5))
                {
                    debugger.SendCommand("go");
                }
                if (ImGui.IsKeyPressed(ImGuiKey.F7))
                {
                    debugger.SendCommand("s");
                }
                if (ImGui.IsKeyPressed(ImGuiKey.F8))
                {
                    debugger.SendCommand("o");
                }
            }
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


        ImGui.EndChild();
        ImGui.PopStyleVar(2);

        var size = ImGui.GetWindowSize();
        size.Y -= YOff;
        if (size.Y > 0)
        {
            var expectedSize = (int)Math.Floor(size.Y / sizeOfMonoText.Y) - 2;
            if (view.view.H != expectedSize)
            {
                view.view.H = expectedSize;
                view.state = new byte[view.view.W * view.view.H * 2];
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