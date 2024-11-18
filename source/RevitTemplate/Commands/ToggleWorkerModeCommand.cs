using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using RevitTemplate.Http.Service;

namespace RevitTemplate.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class ToggleWorkerModeCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var httpServer = Host.GetService<HttpServer>();
        
        try
        {
            var turnedOn = Application.ToggleWorkerMode();
            if (turnedOn)
            {
                httpServer.Start();
                Console.WriteLine("Server started, listening..");
            }
            else
            {
                httpServer.Stop();
                Console.WriteLine("Server has been stopped.");
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Failed to start Worker Mode. Exception: {e.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            return Result.Failed;
        }

        return Result.Succeeded;
    }
}