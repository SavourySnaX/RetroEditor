
public delegate void MenuEventHandler(IEditor editorInterface, IMenuItem menu);

public interface IMenuItem
{
    bool Enabled { get; set; }
    string Name { get; set; }
    MenuEventHandler? Handler { get; set; }
}

public interface IMenu
{
    IMenuItem AddItem(string name);
    IMenuItem AddItem(string name, MenuEventHandler handler);
    IMenuItem AddItem(IMenuItem parent, string name, MenuEventHandler handler);
}

public interface IMenuProvider
{
    void ConfigureMenu(IRomAccess rom, IMenu menu);
}