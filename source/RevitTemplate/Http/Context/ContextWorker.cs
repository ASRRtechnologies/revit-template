using RevitTemplate.Http.Service;

namespace RevitTemplate.Http.Context;

public class ContextWorker(ContextQueue contextQueue)
{
    private const int IdleSleepDuration = 300;
    private readonly HttpRequestHandler _httpRequestHandler = new();

    public void Run()
    {
        if (contextQueue.IsEmpty())
        {
            Thread.Sleep(IdleSleepDuration);
            return;
        }

        var context = contextQueue.Dequeue();
        _httpRequestHandler.Process(context.Request, context.Response);
    }
}