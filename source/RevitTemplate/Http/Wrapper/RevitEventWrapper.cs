using Autodesk.Revit.UI;

namespace RevitTemplate.Http.Wrapper;

public abstract class RevitEventWrapper<T> : IExternalEventHandler
{
    private readonly ExternalEvent _revitEvent;
    private readonly object _lock;
    private T _savedArgs;

    protected RevitEventWrapper()
    {
        _revitEvent = ExternalEvent.Create(this);
        _lock = new object();
    }

    public void Execute(UIApplication app)
    {
        T args;

        lock (_lock)
        {
            args = _savedArgs;
            _savedArgs = default(T);
        }

        Execute(app, args);
    }

    public string GetName()
    {
        return GetType().Name;
    }

    public void Raise(T args)
    {
        lock (_lock)
        {
            _savedArgs = args;
        }

        _revitEvent.Raise();
    }

    public abstract void Execute(UIApplication app, T args);
}