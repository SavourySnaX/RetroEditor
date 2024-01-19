
using ImGuiNET;

internal class WindowManager
{
    private List<WindowWrapper> activeWindows;
    private PriorityQueue<UpdateQueueWrapper, float> priorityQueue;
    private float totalTime;

    private struct WindowWrapper
    {
        public IWindow Window;
        public string Name;
        public bool modalPopup;
    }

    private struct UpdateQueueWrapper
    {
        public WindowWrapper Window;
        public Func<WindowWrapper,float,bool> Action;
        public float Time;
    }

    public WindowManager()
    {
        activeWindows = new List<WindowWrapper>();
        priorityQueue = new PriorityQueue<UpdateQueueWrapper, float>();
        totalTime = 0.0f;
    }

    public void AddWindow(IWindow window, string name)
    {
        var newWindow = new WindowWrapper
        {
            Window = window,
            Name = name,
            modalPopup = false
        };
        activeWindows.Add(newWindow);
        var newTime = totalTime + window.UpdateInterval;
        priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalUpdate, Time = newTime }, newTime);
    }

    public void AddBlockingPopup(IWindow window, string name)
    {
        var newWindow = new WindowWrapper
        {
            Window = window,
            Name = name,
            modalPopup = true
        };
        activeWindows.Add(newWindow);
        var newTime = totalTime + window.UpdateInterval;
        priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalPopup, Time = totalTime }, totalTime);
        priorityQueue.Enqueue(new UpdateQueueWrapper { Window = newWindow, Action = InternalUpdate, Time = newTime }, newTime);
    }

    public void Update(float deltaTime)
    {
        totalTime += deltaTime;
        // Update all windows in the priority queue
        if (priorityQueue.Count > 0)
        {
            while (priorityQueue.Peek().Time <= totalTime)
            {
                var next = priorityQueue.Dequeue();
                if (!next.Action(next.Window, totalTime))
                {
                    var diff = Math.Min(next.Time, totalTime - next.Time);
                    var newTime = totalTime + next.Window.Window.UpdateInterval - diff;
                    next.Time = newTime;
                    priorityQueue.Enqueue(next, newTime);
                }
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
        return false;
    }

    private bool InternalPopup(WindowWrapper window, float totalTime)
    {
        ImGui.OpenPopup(window.Name);
        return true;
    }

    private bool InternalDraw(WindowWrapper window)
    {
        bool open = true;
        if (window.modalPopup)
        {
            if (ImGui.BeginPopupModal(window.Name, ref open))
            {
                if (window.Window.Draw())
                {
                    ImGui.CloseCurrentPopup();
                    open = false;
                }
                ImGui.EndPopup();
            }
        }
        else
        {
            ImGui.Begin(window.Name, ref open);
            if (window.Window.Draw())
            {
                ImGui.CloseCurrentPopup();
                open = false;
            }
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

}