using System.Net;
using Newtonsoft.Json.Linq;
using RevitTemplate.Http.Model;
using RevitTemplate.Http.Service;
using RevitTemplate.Http.Wrapper;

namespace RevitTemplate.Http.RequestHandler;

public static class ExecuteRequestHandler
{
    public static readonly string[] AllowedHttpMethods = ["POST"];

    public static void Process(JObject content, HttpResponseWrapper response, ServerStatus serverStatus,
        ExecuteRequestProcessor requestProcessor, IExternalEventExecutor externalEventExecutor)
    {
        var queueId = GetQueueId(content, response);
        if (queueId == null) return;

        if (!serverStatus.Busy)
        {
            serverStatus.Busy = true;
            Application.UpdateWorkerStatus(true);
            requestProcessor.Process(queueId, serverStatus, externalEventExecutor);
        }

        response.WriteBody(serverStatus);
    }

    private static string GetQueueId(JObject jObject, HttpResponseWrapper response)
    {
        if (HasProperty(jObject, "queueId"))
        {
            var queueId = (string) jObject["queueId"];
            return string.IsNullOrEmpty(queueId) ? null : queueId;
        }

        response.WriteError(HttpStatusCode.BadRequest, "Body does not have field 'queueId'");
        return null;
    }

    private static bool HasProperty(JObject jObject, string property)
    {
        return jObject[property] != null;
    }
}