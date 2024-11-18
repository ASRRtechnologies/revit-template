using RevitTemplate.Http.Service;

namespace RevitTemplate.Http.Model;

public class RevitParameters(string queueId, ServerStatus serverStatus, IExternalEventExecutor externalEventExecutor)
{
    public readonly string QueueId = queueId;
    public readonly ServerStatus ServerStatus = serverStatus;
    public readonly IExternalEventExecutor ExternalEventExecutor = externalEventExecutor;
}