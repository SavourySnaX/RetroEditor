namespace RetroEditor.Plugins
{
    /// <summary>
    /// Represents a 3D vector with float components
    /// </summary>
    public struct Vector3F
    {
        /// <summary>
        /// X component of the vector
        /// </summary>
        public float x;
        /// <summary>
        /// Y component of the vector
        /// </summary>
        public float y;
        /// <summary>
        /// Z component of the vector
        /// </summary>
        public float z;

        /// <summary>
        /// Initializes a new instance of the Vector3F struct with the specified X, Y, and Z values
        /// </summary>
        public Vector3F(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
    
    /// <summary>
    /// Represents a color with four components (Red, Green, Blue, Alpha) using byte values
    /// </summary>
    public struct Color4B
    {
        /// <summary>
        /// Red component of the color
        /// </summary>
        public byte R;
        /// <summary>
        /// Green component of the color
        /// </summary>
        public byte G;
        /// <summary>
        /// Blue component of the color
        /// </summary>
        public byte B;
        /// <summary>
        /// Alpha component of the color
        /// </summary>
        public byte A;

        /// <summary>
        /// Initializes a new instance of the Color4B struct with the specified RGBA values
        /// </summary>
        public Color4B(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }

    /// <summary>
    /// Represents a triangle in 3D space defined by three vertices
    /// </summary>
    public struct Triangle
    {
        /// <summary>
        /// First vertex of the triangle
        /// </summary>
        public Vector3F Vertex1;
        /// <summary>
        /// Second vertex of the triangle
        /// </summary>
        public Vector3F Vertex2;
        /// <summary>
        /// Third vertex of the triangle
        /// </summary>
        public Vector3F Vertex3;
        /// <summary>
        /// Color of the triangle, represented as a Color4B struct
        /// </summary>
        public Color4B Color;

        /// <summary>
        /// Initializes a new instance of the Triangle struct with the specified vertices
        /// </summary>
        public Triangle(Vector3F v1, Vector3F v2, Vector3F v3, Color4B color)
        {
            Vertex1 = v1;
            Vertex2 = v2;
            Vertex3 = v3;
            Color = color;
        }
    }

    /// <summary>
    /// Interface for a 3D widget that renders a 3D scene
    /// </summary>
    public interface IRender3DWidget
    {
        /// <summary>
        /// Width in pixels of the final rendered image
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Height in pixels of the final rendered image
        /// </summary>
        uint Height { get; }

        /// <summary>
        /// Camera position in the 3D scene
        /// </summary>
        Vector3F CameraPosition { get; }
        /// <summary>
        /// Camera look-at point in the 3D scene
        /// </summary>
        Vector3F CameraLookAt { get; }
        /// <summary>
        /// Camera up vector in the 3D scene
        /// </summary>
        Vector3F CameraUp { get; }
        /// <summary>
        /// Camera field of view in the Y direction (in degrees)
        /// </summary>
        float CameraFovY { get; }
        /// <summary>
        /// Whether the camera is orthographic (true) or perspective (false)
        /// </summary>
        bool CameraOrthographic { get; }

        /// <summary>
        /// Array of triangles to render in the 3D scene
        /// </summary>
        Triangle[] Triangles { get; }

        //TODO : Camera, Objects, Lights, etc
    }
}
