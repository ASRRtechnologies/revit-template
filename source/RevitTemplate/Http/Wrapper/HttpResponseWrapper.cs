using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RevitTemplate.Http.RequestHandler;
using RevitTemplate.Http.Service;

namespace RevitTemplate.Http.Wrapper;

public class HttpResponseWrapper(HttpListenerResponse response)
{
    private bool _responded;
    
    public void AddHeader(string key, string value)
    {
        response.AddHeader(key, value);
    }
    
    public void WriteError(HttpStatusCode statusCode, string message)
    {
        WriteMessage(statusCode, message);
    }
    
    public void WriteInfo(string message)
    {
        WriteMessage(HttpStatusCode.OK, message);
    }

    public void WriteMessage(HttpStatusCode statusCode, string message)
    {
        var body = new JObject {["message"] = message};
        WriteRequest(statusCode, body);
    }

    public void WriteBody(object body)
    {
        var serializer = new JsonSerializer()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        WriteBody(JObject.FromObject(body, serializer));
    }

    public void WriteBody(JObject jObject)
    {
        WriteRequest(HttpStatusCode.OK, jObject);
    }
    
    public void WriteRequest(HttpStatusCode statusCode, JObject body)
    {
        if (_responded)
        {
            // _logger.Error("Cannot respond twice to the same request");
            throw new InvalidOperationException("Cannot respond twice to the same request");
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes(body.ToString());
            response.StatusCode = (int) statusCode;
            response.ContentType = HttpRequestHandler.JsonMimeType;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            _responded = true;
        }
        catch (Exception e)
        {
            // _logger.Error(e.Message);
        }
    }
}