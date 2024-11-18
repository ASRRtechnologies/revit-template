using Autodesk.Revit.UI;

namespace RevitTemplate.Http.Service;

public interface IExternalEventExecutor
{
    void Execute(UIApplication uiApp, string queueId);
}