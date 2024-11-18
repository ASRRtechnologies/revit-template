using System.Net;
using RevitTemplate.Http.Context;
using RevitTemplate.Http.Model;

namespace RevitTemplate.Http.Service;

public class HttpServer
{
    private readonly ServerStatus _serverStatus;
    private readonly HttpListener _listener;
    private readonly ExecuteRequestProcessor _requestProcessor;
    private readonly IExternalEventExecutor _externalEventExecutor;

    public HttpServer(string url, ExecuteRequestProcessor requestProcessor,
        IExternalEventExecutor externalEventExecutor)
    {
        _serverStatus = new ServerStatus();
        _listener = new HttpListener();
        _listener.Prefixes.Add(url);

        _requestProcessor = requestProcessor ?? throw new ArgumentNullException(nameof(requestProcessor));
        _externalEventExecutor = externalEventExecutor
                                 ?? throw new ArgumentNullException(nameof(externalEventExecutor));
    }

    public void Start()
    {
        if (IsListening()) return;

        var contextQueue = new ContextQueue(10);
        var contextWorker = new ContextWorker(contextQueue, _serverStatus, _requestProcessor, _externalEventExecutor);

        _listener.Start();

        new Thread(() =>
        {
            while (IsListening()) contextWorker.Run();
        }).Start();

        new Thread(() =>
        {
            while (IsListening())
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
        if (!IsListening()) return;
        _listener.Stop();
        _serverStatus.Busy = false;
        _serverStatus.ExceptionThrown = false;
    }

    public bool IsListening()
    {
        return _listener.IsListening;
    }
}