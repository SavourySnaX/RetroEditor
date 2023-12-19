
using System.Numerics;
using ImGuiNET;
using Veldrid;

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

    Texture[] bitmap = new Texture[3];
    nint[] bitmapId = new nint[3];

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
        ImGui.BeginChild($"State - {title}", new Vector2(sizeOfMonoText.X*(view.w+2),sizeOfMonoText.Y*(view.h+2)), true, 0);

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
        
        ImGui.Image(bitmapId[0], new Vector2(256 * 1.0f, 192 * 1.0f));
        ImGui.SameLine();
        ImGui.Image(bitmapId[1], new Vector2(256 * 1.0f, 192 * 1.0f));
        ImGui.SameLine();
        ImGui.Image(bitmapId[2], new Vector2(256 * 1.0f, 192 * 1.0f));

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

        ImGui.BeginChild("Log", new Vector2(0, 0), true, ImGuiWindowFlags.HorizontalScrollbar|ImGuiWindowFlags.AlwaysVerticalScrollbar);
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

    public bool Initialise(ImGuiController controller, GraphicsDevice graphicsDevice)
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

        bitmap[0]=graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(256, 192, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        bitmap[0].Name = $"Screen-SpectrumPlane";
        bitmapId[0] = controller.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, bitmap[0]);
        bitmap[1]=graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(256, 192, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        bitmap[1].Name = $"Screen-SpectrumAttr";
        bitmapId[1] = controller.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, bitmap[1]);
        bitmap[2]=graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(256, 192, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
        bitmap[2].Name = $"Screen-SpectrumCombined";
        bitmapId[2] = controller.GetOrCreateImGuiBinding(graphicsDevice.ResourceFactory, bitmap[2]);

        var connected= client.Connect();
        if (connected)
        {
            client.SendMemory(0x9e6d, new byte[] { 0x00 });         // patch start to auto press enter
            //client.SendMemory(0x8297, new byte[] { 0x01, 0x00 }); 
            //client.SendMemory(0x890A, new byte[] { 0x01, 0x00 }); 
            client.SendMemory(0x8C7C, new byte[] { 0x01 });     //screen
            client.SendMemory(0x8C81, new byte[] { 0x01 });     //floor
            client.SendCommand("bpclear");
            client.SendCommand("bp 0xa269");                        // for tracing level setup
            //client.SendCommand("bp 0x8270");
        }
        return connected;
    }

    public void Update(ImGuiController controller, GraphicsDevice graphicsDevice, float seconds)
    {
        if ((forceRefresh) || (seconds - lastSeconds > 1.0f))
        {
            lastSeconds = seconds;
            running = client.IsRunning();
            state.state = client.RequestState(state.x, state.y, state.w, state.h);
            disasm.state = client.RequestDisasm(disasm.x, disasm.y, disasm.w, disasm.h);

            screen = client.RequestMemory(16384, 8192+768);
            RgbaByte[] bitmapData = new RgbaByte[256*192];
            RgbaByte[] attrData = new RgbaByte[256*192];
            RgbaByte[] combinedData = new RgbaByte[256*192];

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
                                    bitmapData[linearY * 256 + linearX + b] = new RgbaByte(0, 0, 0, 255);
                                    combinedData[linearY * 256 + linearX + b] = paperCol;
                                }
                                else
                                {
                                    bitmapData[linearY * 256 + linearX + b] = new RgbaByte(255, 255, 255, 255);
                                    combinedData[linearY * 256 + linearX + b] = inkCol;
                                }
                            }
                        }
                    }
                }
            }
            graphicsDevice.UpdateTexture(bitmap[0], bitmapData, 0, 0, 0, 256, 192, 1, 0, 0);
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
                                attrData[(row * 8 + y) * 256 + (col * 8 + x)] = inkCol;
                            else
                                attrData[(row * 8 + y) * 256 + (col * 8 + x)] = paperCol;
                        }
                    }
                }
            }
            graphicsDevice.UpdateTexture(bitmap[1], attrData, 0, 0, 0, 256, 192, 1, 0, 0);
            graphicsDevice.UpdateTexture(bitmap[2], combinedData, 0, 0, 0, 256, 192, 1, 0, 0);
        }
        forceRefresh = false;
    }

    static readonly RgbaByte[] palette = new RgbaByte[]
    {
        new RgbaByte(0, 0, 0 ,255),
        new RgbaByte(0, 0, 192 ,255),
        new RgbaByte(192, 0, 0 ,255),
        new RgbaByte(192, 0, 192 ,255),
        new RgbaByte(0, 192, 0 ,255),
        new RgbaByte(0, 192, 192 ,255),
        new RgbaByte(192, 192, 0 ,255),
        new RgbaByte(192, 192, 192 ,255),
        new RgbaByte(0, 0, 0 ,255),
        new RgbaByte(0, 0, 255 ,255),
        new RgbaByte(255, 0, 0 ,255),
        new RgbaByte(255, 0, 255 ,255),
        new RgbaByte(0, 255, 0 ,255),
        new RgbaByte(0, 255, 255 ,255),
        new RgbaByte(255, 255, 0 ,255),
        new RgbaByte(255, 255, 255 ,255)
    };


    public void Close(ImGuiController controller, GraphicsDevice graphicsDevice)
    {
        client.Disconnect();
    }
}