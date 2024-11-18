using RevitTemplate.Http.Wrapper;

namespace RevitTemplate.Http.RequestHandler;

public static class HealthRequestHandler
{
    public static readonly string[] AllowedHttpMethods = ["GET"];

    public static void Process(HttpResponseWrapper response)
    {
        response.WriteInfo("Server Running");
    }
}