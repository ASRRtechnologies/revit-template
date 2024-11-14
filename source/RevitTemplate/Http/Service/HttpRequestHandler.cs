using System.Net;
using RevitTemplate.Http.Wrapper;

namespace RevitTemplate.Http.Service;

public class HttpRequestHandler
{
    public const string JsonMimeType = "application/json";
    private static readonly string[] AllowedHttpMethods = ["GET"];

    public void Process(HttpListenerRequest request, HttpListenerResponse httpResponse)
    {
        var response = new HttpResponseWrapper(httpResponse);

        if (GetRequestPath(request) == "health" && MethodIsAllowed(request.HttpMethod, response))
        {
            response.WriteInfo("Server Running");
        }
        else
        {
            // _logger.Error($"Endpoint '{request.Url.AbsolutePath}' not supported");
            response.WriteError(HttpStatusCode.NotFound, "Endpoint not supported");
        }
    }
    
    private static string GetRequestPath(HttpListenerRequest request)
    {
        return request.Url.AbsolutePath.Replace("/", "");
    }
    
    private bool MethodIsAllowed(string method, HttpResponseWrapper response)
    {
        if (method != null && AllowedHttpMethods.Contains(method)) return true;

        // _logger.Error("Invalid HTTP method");
        response.AddHeader("Allow", string.Join(", ", AllowedHttpMethods));
        response.WriteError(HttpStatusCode.MethodNotAllowed, "Invalid HTTP method");
        return false;
    }
}