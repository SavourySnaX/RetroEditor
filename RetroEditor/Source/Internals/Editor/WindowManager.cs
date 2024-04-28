
using ImGuiNET;
using RetroEditor.Plugins;

internal class WindowManager
{
    private List<WindowWrapper> activeWindows;
    private PriorityQueue<UpdateQueueWrapper, float> priorityQueue;
    private Dictionary<ProjectSettings, List<WindowWrapper>> projectWindows;
    private float totalTime;

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

    public WindowManager()
    {
        activeWindows = new List<WindowWrapper>();
        priorityQueue = new PriorityQueue<UpdateQueueWrapper, float>();
        projectWindows = new Dictionary<ProjectSettings, List<WindowWrapper>>();
        totalTime = 0.0f;
    }

    public void AddWindow(IWindow window, string name, ActiveProject? activeProject)
    {
        var newWindow = new WindowWrapper(window, name , false);
        activeWindows.Add(newWindow);

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
        if (!projectWindows.ContainsKey(settings))
        {
            projectWindows.Add(settings, new List<WindowWrapper>());
        }
        projectWindows[settings].Add(newWindow);
        priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalUpdate, Time = totalTime }, totalTime);
    }

    public void AddBlockingPopup(IWindow window, string name)
    {
        var newWindow = new WindowWrapper(window, name, true);
        activeWindows.Add(newWindow);
        priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalPopup, Time = totalTime }, totalTime);
        priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalUpdate, Time = totalTime }, totalTime);
    }

    public void Update(float deltaTime)
    {
        totalTime += deltaTime;
        // Update all windows in the priority queue
        while (priorityQueue.Count > 0 && priorityQueue.Peek().Time <= totalTime)
        {
            var next = priorityQueue.Dequeue();
            if (activeWindows.IndexOf(next.Window) == -1)
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
                priorityQueue.Enqueue(next, newTime);
            }
        }
    }

    public void Draw()
    {
        foreach (var window in activeWindows)
        {
            if (!InternalDraw(window))
            {
                window.Window.Close();
                activeWindows.Remove(window);
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
        foreach (var widget in window.Widgets)
        {
            widget.Update(totalTime);
        }
    }

    private void DrawWidgets(WindowWrapper window)
    {
        foreach (var widget in window.Widgets)
        {
            widget.Draw();
        }
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
        foreach (var window in activeWindows)
        {
            window.Window.Close();
        }

        activeWindows.Clear();
    }

    internal bool IsOpen(string v)
    {
        foreach (var window in activeWindows)
        {
            if (window.Name == v)
                return true;
        }
        return false;
    }

    private void CloseWindow(WindowWrapper window)
    {
        foreach (var kp in projectWindows)
        {
            if (kp.Value.Contains(window))
            {
                kp.Value.Remove(window);
            }
        }
        window.Window.Close();
        activeWindows.Remove(window);
    }

    internal void Close(string name)
    {
        foreach (var window in activeWindows)
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
        if (projectWindows.ContainsKey(activeProject))
        {
            List<WindowWrapper> windowsToClear = new List<WindowWrapper>();
            foreach (var window in projectWindows[activeProject])
            {
                windowsToClear.Add(window);
            }
            foreach (var window in windowsToClear)
            {
                CloseWindow(window);
            }
            projectWindows[activeProject].Clear();
        }
    }
}