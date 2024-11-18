using Autodesk.Revit.UI;
using RevitTemplate.Http.Model;
using RevitTemplate.Http.Wrapper;

namespace RevitTemplate.Http.Service;

public class ExecuteRequestProcessor : RevitEventWrapper<RevitParameters>
{
    public override void Execute(UIApplication app, RevitParameters args)
    {
        var serverStatus = args.ServerStatus;
        // _logger.Info("Starting configuration export");
        try
        {
            args.ExternalEventExecutor.Execute(app, args.QueueId);
            // _logger.Info(configurationStatus.CollectionPath + " External access");
        }
        catch (Exception e)
        {
            serverStatus.Busy = false;
            serverStatus.ExceptionThrown = true;
            Console.WriteLine(e.Message);
            Application.UpdateWorkerStatus(false);
        }

        serverStatus.Busy = false;
        Application.UpdateWorkerStatus(false);
        // _logger.Info("Busy set to " + serverStatus.Busy + " External access");
    }

    public void Process(string queueId, ServerStatus serverStatus, IExternalEventExecutor externalEventExecutor)
    {
        var revitParameters = new RevitParameters(queueId, serverStatus, externalEventExecutor);
        try
        {
            // _logger.Info("Raising External Export Event");
            Raise(revitParameters);
        }
        catch (Exception)
        {
            serverStatus.Busy = false;
            serverStatus.ExceptionThrown = true;
            Application.UpdateWorkerStatus(false);
        }
    }
}