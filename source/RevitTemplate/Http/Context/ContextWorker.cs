using RevitTemplate.Http.Model;
using RevitTemplate.Http.RequestHandler;
using RevitTemplate.Http.Service;

namespace RevitTemplate.Http.Context;

public class ContextWorker(
    ContextQueue contextQueue,
    ServerStatus serverStatus,
    ExecuteRequestProcessor requestProcessor,
    IExternalEventExecutor externalEventExecutor)
{
    private const int IdleSleepDuration = 300;

    public void Run()
    {
        if (contextQueue.IsEmpty())
        {
            Thread.Sleep(IdleSleepDuration);
            return;
        }

        var context = contextQueue.Dequeue();
        HttpRequestHandler.Process(context.Request, context.Response, serverStatus, requestProcessor,
            externalEventExecutor);
    }
}