using RevitTemplate.Http.Model;
using RevitTemplate.Http.Wrapper;

namespace RevitTemplate.Http.RequestHandler;

public static class StatusRequestHandler
{
    public static readonly string[] AllowedHttpMethods = ["GET"];

    public static void Process(HttpResponseWrapper response, ServerStatus status)
    {
        response.WriteBody(status);
    }
}