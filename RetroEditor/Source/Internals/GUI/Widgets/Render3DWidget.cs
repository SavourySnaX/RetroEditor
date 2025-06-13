using System.Numerics;
using Raylib_cs;
using RetroEditor.Plugins;
using rlImGui_cs;

internal class Render3DWidget : IWidgetItem, IWidgetUpdateDraw
{
    IRender3DWidget _widget;
    RenderTexture2D renderTexture;

    private Camera3D _camera;

    public Render3DWidget(IRender3DWidget render3DWidget)
    {
        _widget = render3DWidget;
        renderTexture = Raylib.LoadRenderTexture((int)render3DWidget.Width, (int)render3DWidget.Height);

        _camera = new Camera3D();
        _camera.Position = new Vector3(_widget.CameraPosition.x, _widget.CameraPosition.y, _widget.CameraPosition.z);
        _camera.Target = new Vector3(_widget.CameraLookAt.x, _widget.CameraLookAt.y, _widget.CameraLookAt.z);
        _camera.Up = new Vector3(_widget.CameraUp.x, _widget.CameraUp.y, _widget.CameraUp.z);
        _camera.Projection = CameraProjection.Perspective;
        _camera.FovY = 45.0f;
    }

    public void Update(IWidgetLog logger, float seconds)
    {
    }

    public void Draw(IWidgetLog logger)
    {
        Raylib.UpdateCamera(ref _camera, CameraMode.Orbital);

        Raylib.BeginTextureMode(renderTexture);
        Raylib.ClearBackground(Color.Beige);

        Raylib.BeginMode3D(_camera);
        Rlgl.DisableBackfaceCulling();

        foreach (var triangle in _widget.Triangles)
        {
            var v1 = new Vector3(triangle.Vertex1.x, triangle.Vertex1.y, triangle.Vertex1.z);
            var v2 = new Vector3(triangle.Vertex2.x, triangle.Vertex2.y, triangle.Vertex2.z);
            var v3 = new Vector3(triangle.Vertex3.x, triangle.Vertex3.y, triangle.Vertex3.z);

            Raylib.DrawTriangle3D(v1, v2, v3, new Color(triangle.Color.R, triangle.Color.G, triangle.Color.B, triangle.Color.A));
        }

        foreach (var line in _widget.Lines)
        {
            var v1 = new Vector3(line.Vertex1.x, line.Vertex1.y, line.Vertex1.z);
            var v2 = new Vector3(line.Vertex2.x, line.Vertex2.y, line.Vertex2.z);

            Raylib.DrawLine3D(v1, v2, new Color(line.Color.R, line.Color.G, line.Color.B, line.Color.A));
        }

        foreach (var point in _widget.Points)
        {
            var position = new Vector3(point.Position.x, point.Position.y, point.Position.z);
            Raylib.DrawPoint3D(position, new Color(point.Color.R, point.Color.G, point.Color.B, point.Color.A));
        }

        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        rlImGui.ImageRenderTexture(renderTexture);
    }
}

