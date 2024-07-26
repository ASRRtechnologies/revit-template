using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;

namespace RevitTemplate.Commands;

[UsedImplicitly]
[Transaction(TransactionMode.Manual)]
public class PopupCommand : ExternalCommand
{
    public override void Execute()
    {
        try
        {
            TaskDialog.Show("ASRR Sample Add-in", "If you see this, the add-in works!");
        }
        catch (Exception e)
        {
            TaskDialog.Show("ASRR Sample Add-in", $"Exception found: {e.Message}");
        }
    }
}