using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RevitTemplate.Http.Service;

namespace RevitTemplate.Http.Wrapper;

public class HttpResponseWrapper
{
    private readonly HttpListenerResponse _response;
    private bool _responded;

    public HttpResponseWrapper(HttpListenerResponse response)
    {
        _response = response;
        _responded = false;
    }

    public bool HasResponded()
    {
        return _responded;
    }
    
    public void AddHeader(string key, string value)
    {
        _response.AddHeader(key, value);
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
            _response.StatusCode = (int) statusCode;
            _response.ContentType = HttpRequestHandler.JsonMimeType;
            _response.ContentLength64 = buffer.Length;
            _response.OutputStream.Write(buffer, 0, buffer.Length);
            _responded = true;
        }
        catch (Exception e)
        {
            // _logger.Error(e.Message);
        }
    }
}