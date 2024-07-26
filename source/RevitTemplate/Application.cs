using Nice3point.Revit.Toolkit.External;
using RevitTemplate.Commands;

namespace RevitTemplate;

[UsedImplicitly]
public class Application : ExternalApplication
{
    public override void OnStartup()
    {
        Host.Start();
        CreateRibbon();
    }

    private void CreateRibbon()
    {
        var panel = Application.CreatePanel("Commands", "ASRR");

        panel.AddPushButton<PopupCommand>("Click Me!")
            .SetImage("/RevitTemplate;component/Resources/Icons/RibbonIcon16.png")
            .SetLargeImage("/RevitTemplate;component/Resources/Icons/RibbonIcon32.png");
    }
}