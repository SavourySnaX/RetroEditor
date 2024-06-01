
using ImGuiNET;
using RetroEditor.Plugins;

internal class WindowManager : IWidgetLog
{
    private List<WindowWrapper> _activeWindows;
    private PriorityQueue<UpdateQueueWrapper, float> _priorityQueue;
    private Dictionary<ProjectSettings, List<WindowWrapper>> _projectWindows;
    private Dictionary<WindowWrapper, ProjectSettings> _windowProjects;
    private float totalTime;
    private Editor _editor;
    private string _activeWindowProjectName = "Unknown";


    private class WindowWrapper
    {
        public WindowWrapper(IWindow window, string name, bool modalPopup)
        {
            _window = window;
            _name = name;
            _modalPopup = modalPopup;
            _widgetFactory = new WidgetFactory();
        }

        private IWindow _window;
        private string _name;
        private bool _modalPopup;
        private WidgetFactory _widgetFactory;

        public IWindow Window => _window;
        public string Name => _name;
        public bool ModalPopup => _modalPopup;
        public IEnumerable<IWidgetUpdateDraw> Widgets => _widgetFactory.Widgets;
        public WidgetFactory WidgetFactory => _widgetFactory;
    }

    private struct UpdateQueueWrapper
    {
        public WindowWrapper Window;
        public Func<WindowWrapper,float,bool> Action;
        public float Time;
    }

    private ProjectSettings developerDummyProject = new ProjectSettings("Developer", "", "", "", "");

    public WindowManager(Editor editor)
    {
        _editor = editor;
        _activeWindows = new List<WindowWrapper>();
        _priorityQueue = new PriorityQueue<UpdateQueueWrapper, float>();
        _projectWindows = new Dictionary<ProjectSettings, List<WindowWrapper>>();
        _windowProjects = new Dictionary<WindowWrapper, ProjectSettings>();
        totalTime = 0.0f;
    }

    public void AddWindow(IWindow window, string name, ActiveProject? activeProject)
    {
        var newWindow = new WindowWrapper(window, name , false);
        _activeWindows.Add(newWindow);

        var settings = developerDummyProject;
        if (activeProject != null)
        {
            settings = activeProject.Value.Settings;
            if (activeProject.Value.RetroPlugin is IPlayerWindowExtension playerWindowExtension &&
                window is LibRetroPlayerWindow)
            {
                playerWindowExtension.ConfigureWidgets(activeProject.Value.PlayableRomPlugin, newWindow.WidgetFactory, activeProject);
            }
            if (window is UserWindow userWindow)
            {
                userWindow.UserWindowInterface.ConfigureWidgets(activeProject.Value.PlayableRomPlugin, newWindow.WidgetFactory, activeProject);
            }
        }
        if (!_projectWindows.ContainsKey(settings))
        {
            _projectWindows.Add(settings, new List<WindowWrapper>());
        }
        _projectWindows[settings].Add(newWindow);
        _windowProjects.Add(newWindow, settings);
        _priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalUpdate, Time = totalTime }, totalTime);
    }

    public void AddBlockingPopup(IWindow window, string name)
    {
        var newWindow = new WindowWrapper(window, name, true);
        _activeWindows.Add(newWindow);
        _priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalPopup, Time = totalTime }, totalTime);
        _priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalUpdate, Time = totalTime }, totalTime);
    }

    public void Update(float deltaTime)
    {
        totalTime += deltaTime;
        // Update all windows in the priority queue
        while (_priorityQueue.Count > 0 && _priorityQueue.Peek().Time <= totalTime)
        {
            var next = _priorityQueue.Dequeue();
            if (_activeWindows.IndexOf(next.Window) == -1)
            {
                continue;
            }
            if (!next.Action(next.Window, totalTime))
            {
                var newTime = next.Time + next.Window.Window.UpdateInterval;
                if (newTime < totalTime)
                {
                    // If we failed to keep up, just wait a whole upate now
                    newTime = totalTime + next.Window.Window.UpdateInterval;
                }
                next.Time = newTime;
                _priorityQueue.Enqueue(next, newTime);
            }
        }
    }

    public void Draw()
    {
        foreach (var window in _activeWindows)
        {
            if (!InternalDraw(window))
            {
                window.Window.Close();
                _activeWindows.Remove(window);
                break;
            }
        }
    }

    private bool InternalUpdate(WindowWrapper window, float totalTime)
    {
        window.Window.Update(totalTime);
        UpdateWidgets(window, totalTime);
        return false;
    }

    private bool InternalPopup(WindowWrapper window, float totalTime)
    {
        ImGui.OpenPopup(window.Name);
        return true;
    }

    private void UpdateWidgets(WindowWrapper window, float totalTime)
    {
        var isProjectBasedWindow= _windowProjects.ContainsKey(window);
        if (isProjectBasedWindow)
        {
            _activeWindowProjectName = _windowProjects[window].RetroPluginName;
        }
        else
        {
            _activeWindowProjectName = "System Window";
        }
        foreach (var widget in window.Widgets)
        {
            widget.Update(this, totalTime);
        }
        _activeWindowProjectName = "Unknown";
    }

    private void DrawWidgets(WindowWrapper window)
    {
        var isProjectBasedWindow= _windowProjects.ContainsKey(window);
        if (isProjectBasedWindow)
        {
            _activeWindowProjectName = _windowProjects[window].RetroPluginName;
        }
        else
        {
            _activeWindowProjectName = "System Window";
        }
        foreach (var widget in window.Widgets)
        {
            widget.Draw(this);
        }
        _activeWindowProjectName = "Unknown";
    }

    private bool InternalDraw(WindowWrapper window)
    {
        bool open = true;
        if (window.ModalPopup)
        {
            if (ImGui.BeginPopupModal(window.Name, ref open))
            {
                open&=!window.Window.Draw();
                DrawWidgets(window);
                if (!open)
                {
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }
        }
        else
        {
            ImGui.Begin(window.Name, ref open);
            open&=!window.Window.Draw();
            DrawWidgets(window);
            ImGui.End();
        }
        return open;
    }

    public void CloseAll()
    {
        foreach (var window in _activeWindows)
        {
            window.Window.Close();
        }

        _activeWindows.Clear();
    }

    internal bool IsOpen(string v)
    {
        foreach (var window in _activeWindows)
        {
            if (window.Name == v)
                return true;
        }
        return false;
    }

    private void CloseWindow(WindowWrapper window)
    {
        foreach (var kp in _projectWindows)
        {
            if (kp.Value.Contains(window))
            {
                kp.Value.Remove(window);
            }
        }
        window.Window.Close();
        _windowProjects.Remove(window);
        _activeWindows.Remove(window);
    }

    internal void Close(string name)
    {
        foreach (var window in _activeWindows)
        {
            if (window.Name == name)
            {
                CloseWindow(window);
                break;
            }
        }
    }

    internal void CloseAll(ProjectSettings activeProject)
    {
        if (_projectWindows.ContainsKey(activeProject))
        {
            List<WindowWrapper> windowsToClear = new List<WindowWrapper>();
            foreach (var window in _projectWindows[activeProject])
            {
                windowsToClear.Add(window);
            }
            foreach (var window in windowsToClear)
            {
                CloseWindow(window);
            }
            _projectWindows[activeProject].Clear();
        }
    }

    public void Log(LogType type, string message)
    {
        _editor.Log(type, _activeWindowProjectName, message);
    }
}