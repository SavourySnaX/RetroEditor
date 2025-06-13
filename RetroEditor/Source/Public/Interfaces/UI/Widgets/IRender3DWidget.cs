
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

        /// <summary>
        /// Adds two <see cref="Vector3F"/> instances component-wise.
        /// </summary>
        /// <param name="a">The first vector to add.</param>
        /// <param name="b">The second vector to add.</param>
        /// <returns>A new <see cref="Vector3F"/> that is the sum of <paramref name="a"/> and <paramref name="b"/>.</returns>
        public static Vector3F operator + (Vector3F a, Vector3F b)
        {
            return new Vector3F(a.x + b.x, a.y + b.y, a.z + b.z);
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
    /// Represents a point in 3D space with a position and color
    /// </summary>
    public struct Point
    {
        /// <summary>
        /// Position of the point in 3D space
        /// </summary>
        public Vector3F Position;
        /// <summary>
        /// Color of the point, represented as a Color4B struct
        /// </summary>
        public Color4B Color;
        /// <summary>
        /// Initializes a new instance of the Point struct with the specified position and color
        /// </summary>
        /// <param name="position">Position of the point in 3D space</param>
        /// <param name="color">Color of the point, represented as a Color4B struct</param> 
        public Point(Vector3F position, Color4B color)
        {
            Position = position;
            Color = color;
        }

    }

    /// <summary>
    /// Represents a line in 3D space defined by two vertices and a color
    /// </summary>
    public struct Line
    {
        /// <summary>
        /// First vertex of the line
        /// </summary>
        public Vector3F Vertex1;
        /// <summary>
        /// Second vertex of the line
        /// </summary>
        public Vector3F Vertex2;
        /// <summary>
        /// Color of the line, represented as a Color4B struct
        /// </summary>
        public Color4B Color;

        /// <summary>
        /// Initializes a new instance of the Line struct with the specified vertices and color
        /// </summary>
        /// <param name="v1">First vertex of the line</param>
        /// <param name="v2">Second vertex of the line</param>
        /// <param name="color">Color of the line, represented as a Color4B struct</param>
        public Line(Vector3F v1, Vector3F v2, Color4B color)
        {
            Vertex1 = v1;
            Vertex2 = v2;
            Color = color;
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

        /// <summary>
        /// Array of lines to render in the 3D scene
        /// </summary>
        Line[] Lines { get; }

        /// <summary>
        /// Array of points to render in the 3D scene
        /// </summary>
        Point[] Points { get; }


        //TODO : Camera, Objects, Lights, etc
    }
}
