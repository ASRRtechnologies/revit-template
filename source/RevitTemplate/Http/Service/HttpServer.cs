using System.Net;
using RevitTemplate.Http.Context;

namespace RevitTemplate.Http.Service;

public class HttpServer
{
    private readonly HttpListener _listener;

    public HttpServer(string url)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add(url);
    }

    public void Start()
    {
        if (_listener.IsListening) return;
        
        var contextQueue = new ContextQueue(10);
        var contextWorker = new ContextWorker(contextQueue);
        
        _listener.Start();

        new Thread(() =>
        {
            while (_listener.IsListening) contextWorker.Run();
        }).Start();

        new Thread(() =>
        {
            while (_listener.IsListening)
            {
                try
                {
                    contextQueue.Enqueue(_listener.GetContext());
                }
                catch (HttpListenerException)
                {
                    contextQueue.Dispose();
                }
            }
        }).Start();
        
        // Log.Info($"Started listening at {ServerBaseAddress}",
        //     "Revit worker ready for incoming requests", "positive");
    }

    public void Stop()
    {
        if (_listener.IsListening) _listener.Stop();
    }

    public bool IsListening()
    {
        return _listener.IsListening;
    }
}