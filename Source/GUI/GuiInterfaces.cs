
using System.Threading.Channels;
using ImGuiNET;
using Veldrid;

public interface IWindow
{
    bool Initialise(ImGuiController controller,GraphicsDevice graphicsDevice);
    void Update(ImGuiController controller, GraphicsDevice graphicsDevice, float seconds);
    bool Draw();

    void Close(ImGuiController controller, GraphicsDevice graphicsDevice);
}