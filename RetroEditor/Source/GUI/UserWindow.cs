using RetroEditor.Plugins;

internal class UserWindow : IWindow
{
    private IUserWindow _userWindow;

    public UserWindow(IUserWindow userWindow)
    {
        _userWindow = userWindow;
    }

    internal IUserWindow UserWindowInterface => _userWindow;

    public float UpdateInterval => _userWindow.UpdateInterval;

    public void Close()
    {
        _userWindow.OnClose();
    }

    public void Update(float seconds)
    {
    }

    public bool Draw()
    {
        return false;
    }

    public bool Initialise()
    {
        return true;
    }

}