using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using RevitTemplate.Http.Model;
using RevitTemplate.Http.Service;
using RevitTemplate.Http.Wrapper;

namespace RevitTemplate.Http.RequestHandler;

public static class HttpRequestHandler
{
    public const string JsonMimeType = "application/json";

    public static void Process(HttpListenerRequest request, HttpListenerResponse httpResponse, ServerStatus serverStatus,
        ExecuteRequestProcessor requestProcessor, IExternalEventExecutor externalEventExecutor)
    {
        var response = new HttpResponseWrapper(httpResponse);
        var requestMethod = request.HttpMethod;
        var requestPath = GetRequestPath(request);

        switch (requestPath)
        {
            case "health":
                if (!MethodIsAllowed(requestMethod, HealthRequestHandler.AllowedHttpMethods, response)) return;
                HealthRequestHandler.Process(response);
                break;
            case "status":
                if (!MethodIsAllowed(requestMethod, StatusRequestHandler.AllowedHttpMethods, response)) return;
                StatusRequestHandler.Process(response, serverStatus);
                break;
            case "execute":
                if (!MethodIsAllowed(requestMethod, ExecuteRequestHandler.AllowedHttpMethods, response) || !ContentIsAllowed(request, response)) return;
                var jObject = ReadJObject(request, response);
                if (jObject == null) return;
                ExecuteRequestHandler.Process(jObject, response, serverStatus, requestProcessor, externalEventExecutor);
                break;
            default:
                // _logger.Error($"Endpoint '{request.Url.AbsolutePath}' not supported");
                response.WriteError(HttpStatusCode.NotFound, "Endpoint not supported");
                break;
        }
    }

    private static string GetRequestPath(HttpListenerRequest request)
    {
        return request.Url.AbsolutePath.Replace("/", "");
    }
    
    private static bool MethodIsAllowed(string method, string[] allowedHttpMethods, HttpResponseWrapper response)
    {
        if (method != null && allowedHttpMethods.Contains(method)) return true;

        // _logger.Error("Invalid HTTP method");
        response.AddHeader("Allow", string.Join(", ", allowedHttpMethods));
        response.WriteError(HttpStatusCode.MethodNotAllowed, "Invalid HTTP method");
        return false;
    }
    
    private static bool ContentIsAllowed(HttpListenerRequest request, HttpResponseWrapper response)
    {
        if (request.ContentType != null && !request.ContentType.Equals(JsonMimeType))
        {
            // _logger.Error("Invalid MIME type");
            response.WriteError(HttpStatusCode.BadRequest, "Invalid MIME type");
            return false;
        }

        if (request.HasEntityBody) return true;

        // _logger.Error("Content is empty");
        response.WriteError(HttpStatusCode.BadRequest, "Content is empty");
        return false;
    }

    private static JObject ReadJObject(HttpListenerRequest request, HttpResponseWrapper response)
    {
        try
        {
            string jsonString;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                jsonString = reader.ReadToEnd();
            }

            return JObject.Parse(jsonString);
        }
        catch (Exception e)
        {
            // _logger.Error(e.Message);
            response.WriteError(HttpStatusCode.InternalServerError, "Failed to read content");
            return null;
        }
    }
}