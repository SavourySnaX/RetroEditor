
public interface IWindow
{
    bool Initialise();
    void Update(float seconds);

    float UpdateInterval { get; }

    bool Draw();

    void Close();
}