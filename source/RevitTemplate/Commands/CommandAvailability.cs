using Autodesk.Revit.UI;
using RevitTemplate.Http.Service;

namespace RevitTemplate.Commands;

public class CommandAvailability : IExternalCommandAvailability
{
    public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
    {
        return !Host.GetService<HttpServer>().IsListening();
    }
}