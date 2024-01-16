using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;

// At present, this is hardwired to the Spectrum - make it a plugin, generic later - see MameRemote.cs for mame changes needed

public class MameRemoteCommandWindow : IWindow
{
    public class DView
    {
        public DView()
        {
            state = Array.Empty<byte>();
        }

        public int x, y, w, h;
        public byte[] state;
    }

    MameRemoteClient client;
    string inputBuffer;

    DView state;
    DView disasm;
    byte[] screen=Array.Empty<byte>();      // Obviously specific to the system we are using... hardwired for now

    float lastSeconds;
    string log;
    bool forceRefresh;
    bool running = false;

    Texture2D[] bitmap = new Texture2D[3];

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

    private void DrawDView(DView view, string title)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0,0));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(0,0));
    
        var sizeOfMonoText=ImGui.CalcTextSize("A");
        ImGui.BeginChild($"State - {title}", new Vector2(sizeOfMonoText.X*(view.w+2),sizeOfMonoText.Y*(view.h+2)), 0, 0);

        var drawList = ImGui.GetWindowDrawList();
        var convCode = new byte[] { 0, 0 };
        Vector2 pos = ImGui.GetCursorScreenPos();
        for (int yy=0;yy<view.h;yy++)
        {
            for (int xx=0;xx<view.w;xx++)
            {
                var attr=view.state[(yy*view.w+xx)*2];
                FetchColourForStyle(attr,out var fg,out var bg);
                convCode[0]=view.state[(yy*view.w+xx)*2+1];
                drawList.AddRectFilled(pos, new Vector2(pos.X + sizeOfMonoText.X, pos.Y + sizeOfMonoText.Y), ImGui.GetColorU32(bg));
                drawList.AddText(pos, ImGui.GetColorU32(fg), System.Text.Encoding.ASCII.GetString(convCode));
                pos.X += sizeOfMonoText.X;
            }
            pos.X = ImGui.GetCursorScreenPos().X;
            pos.Y += sizeOfMonoText.Y;
        }
        ImGui.EndChild();
        ImGui.PopStyleVar(2);
    }

    public bool Draw()
    {
        bool open = true;
        ImGui.Begin($"Mame Remote",ref open);

        DrawDView(state,"State");
        ImGui.SameLine();
        DrawDView(disasm,"Disasm");
        
        rlImGui.ImageSize(bitmap[0], 256, 192);
        ImGui.SameLine();
        rlImGui.ImageSize(bitmap[1], 256, 192);
        ImGui.SameLine();
        rlImGui.ImageSize(bitmap[2], 256, 192);

        if (ImGui.InputText("Command",ref inputBuffer, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            log = "";
            inputBuffer.Trim();
            var result = client.SendCommand(inputBuffer);
            foreach (var line in result)
            {
                log += line + "\n";
            }
            inputBuffer = "";
        }

        ImGui.BeginChild("Log", new Vector2(0, 0), 0, ImGuiWindowFlags.HorizontalScrollbar|ImGuiWindowFlags.AlwaysVerticalScrollbar);
        ImGui.Text(log);
        ImGui.EndChild();

        ImGui.End();

        if (!running)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.F5))
            {
                client.SendCommand("go");
                forceRefresh = true;
            }
            if (ImGui.IsKeyPressed(ImGuiKey.F11))
            {
                forceRefresh = true;
                client.SendCommand("step");
            }
            if (ImGui.IsKeyPressed(ImGuiKey.F10))
            {
                forceRefresh = true;
                client.SendCommand("over");
            }
        }
        else
        {
            if (ImGui.IsKeyPressed(ImGuiKey.F5))
            {
                forceRefresh = true;
                client.SendCommand("out");
            }
        }

        return open;
    }

    public MameRemoteCommandWindow()
    {
        state=new DView();
        disasm=new DView();
        client = new MameRemoteClient();
        inputBuffer="";
        log="";
    }

    public bool Initialise()
    {
        state.x = 0;
        state.y = 0;
        state.w = 20;
        state.h = 25;
        disasm.x = 0;
        disasm.y = 0;
        disasm.w = 100;
        disasm.h = 25;
        lastSeconds = 0.0f;
        log = "";
        inputBuffer = "";
        forceRefresh = true;

        var image = new Image {
            Width = 256,
            Height = 192,
            Mipmaps = 1,
            Format = PixelFormat.PIXELFORMAT_UNCOMPRESSED_R8G8B8A8
        };
        bitmap[0] = Raylib.LoadTextureFromImage(image);
        bitmap[1] = Raylib.LoadTextureFromImage(image);
        bitmap[2] = Raylib.LoadTextureFromImage(image);

        var connected= client.Connect();
        if (connected)
        {
            client.SendMemory(0x9e6d, new byte[] { 0x00 });         // patch start to auto press enter
            //client.SendMemory(0x8297, new byte[] { 0x01, 0x00 }); 
            //client.SendMemory(0x890A, new byte[] { 0x01, 0x00 }); 
            client.SendMemory(0x8C7C, new byte[] { 0x01 });     //screen
            client.SendMemory(0x8C81, new byte[] { 0x01 });     //floor

            // Sprite Experiments
            //client.SendMemory(0xC2B8, new byte[] { 0x2D });
            //client.SendMemory(0xC2B8 + 5, new byte[] { 0x07, 0x43 });
            //client.SendMemory(0xC2B8 + 12, new byte[] { 0x30 });
            //client.SendMemory(0xC2B8 + 4, new byte[] { 0x30, 0x07 });
            //client.SendMemory(0x6283, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
            //client.SendCommand("bpclear");
            //client.SendCommand("bp 0xa269");                        // for tracing level setup
            //client.SendCommand("bp 0xADE6");

            // Cheats
            client.SendMemory(36056, new byte[] { 195 });   // Fall any height
            client.SendMemory(36595, new byte[] { 182 });    // Infinite Lives
            client.SendMemory(38342, new byte[] { 0 });    // Infinite Energy
            client.SendMemory(39950, new byte[] { 201 });   // Infinite Energy

        }
        return connected;
    }

    public void Update(float seconds)
    {
        if ((forceRefresh) || (seconds - lastSeconds > 1.0f))
        {
            lastSeconds = seconds;
            running = client.IsRunning();
            state.state = client.RequestState(state.x, state.y, state.w, state.h);
            disasm.state = client.RequestDisasm(disasm.x, disasm.y, disasm.w, disasm.h);

            screen = client.RequestMemory(16384, 8192+768);
            var bitmapData = new byte[4 * 256 * 192];
            var attrData = new byte[4 * 256 * 192];
            var combinedData = new byte[4 * 256 * 192];

            int originalScreenPos = 0;
            for (int block = 0; block < 3; block++)
            {
                for (int cellRow = 0; cellRow < 8; cellRow++)
                {
                    for (int row = 0; row < 8; row++)
                    {
                        for (int col = 0; col < 32; col++)
                        {
                            var linearY = block * 64 + cellRow + row * 8;
                            var linearX = col*8;

                            var attr = screen[0x1800+((linearY/8)*32)+col];
                            var ink = attr & 0x07;
                            var paper = (attr >> 3) & 0x07;
                            var bright = (attr & 0x40) == 0x40 ? 8 : 0;
                            var inkCol = palette[ink + bright];
                            var paperCol = palette[paper + bright];

                            var bits = screen[originalScreenPos++];
                            for (int b = 0; b < 8; b++)
                            {
                                if ((bits & (1 << (7 - b))) == 0)
                                {
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 0] = 0;
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 1] = 0;
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 2] = 0;
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 3] = 255;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 0] = paperCol.Red;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 1] = paperCol.Green;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 2] = paperCol.Blue;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 3] = paperCol.Alpha;
                                }
                                else
                                {
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 0] = 255;
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 1] = 255;
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 2] = 255;
                                    bitmapData[(linearY * 256 + linearX + b) * 4 + 3] = 255;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 0] = inkCol.Red;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 1] = inkCol.Green;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 2] = inkCol.Blue;
                                    combinedData[(linearY * 256 + linearX + b) * 4 + 3] = inkCol.Alpha;
                                }
                            }
                        }
                    }
                }
            }
            originalScreenPos = 0x1800;
            for (int row = 0; row < 24; row++)
            {
                for (int col = 0; col < 32; col++)
                {
                    var attr = screen[originalScreenPos++];
                    var ink = attr & 0x07;
                    var paper = (attr >> 3) & 0x07;
                    var bright = (attr & 0x40) == 0x40 ? 8 : 0;
                    var inkCol = palette[ink + bright];
                    var paperCol = palette[paper + bright];

                    for (int y=0;y<8;y++)
                    {
                        for (int x=0;x<8;x++)
                        {
                            //flash ignored for now
                            if (y >= 2 && y <= 5 && x >= 2 && x <= 5)
                            {
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 0] = inkCol.Red;
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 1] = inkCol.Green;
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 2] = inkCol.Blue;
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 3] = inkCol.Alpha;
                            }
                            else
                            {
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 0] = paperCol.Red;
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 1] = paperCol.Green;
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 2] = paperCol.Blue;
                                attrData[((row * 8 + y) * 256 + (col * 8 + x)) * 4 + 3] = paperCol.Alpha;
                            }
                        }
                    }
                }
            }
            Raylib.UpdateTexture(bitmap[0], bitmapData);
            Raylib.UpdateTexture(bitmap[1], attrData);
            Raylib.UpdateTexture(bitmap[2], combinedData);
        }
        forceRefresh = false;
    }

    public float UpdateInterval => 1.0f / 60.0f;

    static readonly Pixel[] palette = new Pixel[]
    {
        new Pixel{Red=0, Green=0, Blue=0 ,Alpha=255 },
        new Pixel{Red=0, Green=0, Blue=192 ,Alpha=255 },
        new Pixel{Red=192, Green=0, Blue=0 ,Alpha=255 },
        new Pixel{Red=192, Green=0, Blue=192 ,Alpha=255 },
        new Pixel{Red=0, Green=192, Blue=0 ,Alpha=255 },
        new Pixel{Red=0, Green=192, Blue=192 ,Alpha=255 },
        new Pixel{Red=192, Green=192, Blue=0 ,Alpha=255 },
        new Pixel{Red=192, Green=192, Blue=192 ,Alpha=255 },
        new Pixel{Red=0, Green=0, Blue=0 ,Alpha=255 },
        new Pixel{Red=0, Green=0, Blue=255 ,Alpha=255 },
        new Pixel{Red=255, Green=0, Blue=0 ,Alpha=255 },
        new Pixel{Red=255, Green=0, Blue=255 ,Alpha=255 },
        new Pixel{Red=0, Green=255, Blue=0 ,Alpha=255 },
        new Pixel{Red=0, Green=255, Blue=255 ,Alpha=255 },
        new Pixel{Red=255, Green=255, Blue=0 ,Alpha=255 },
        new Pixel{Red=255, Green=255, Blue=255 ,Alpha=255 }
    };


    public void Close()
    {
        client.Disconnect();
    }
}