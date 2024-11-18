using System.Net;

namespace RevitTemplate.Http.Context;

public class ContextQueue(int initialCapacity)
{
    private readonly object _contextQueueLock = new();

    private readonly Queue<HttpListenerContext> _contexts = new(initialCapacity);

    public HttpListenerContext Dequeue()
    {
        lock (_contextQueueLock)
        {
            return _contexts.Dequeue();
        }
    }

    public void Enqueue(HttpListenerContext httpListenerContext)
    {
        lock (_contextQueueLock)
        {
            _contexts.Enqueue(httpListenerContext);
        }
    }

    public bool IsEmpty()
    {
        lock (_contextQueueLock)
        {
            return _contexts.Count == 0;
        }
    }

    public void Dispose()
    {
        lock (_contextQueueLock)
        {
            _contexts.Clear();
        }
    }
}