using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Nice3point.Revit.Toolkit.External;
using RevitTemplate.Commands;
using RevitTemplate.Commands.Settings;

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
        var testPanel = Application.CreatePanel("Test", "ASRR");

        testPanel.AddPushButton<PopupCommand>("Click Me!")
            .SetImage("/RevitTemplate;component/Resources/Icons/RibbonIcon16.png")
            .SetLargeImage("/RevitTemplate;component/Resources/Icons/RibbonIcon32.png");

        var settingsPanel = Application.CreatePanel("Settings", "ASRR");
        var dbSettingsButton = CreatePushButtonData<OpenDatabaseSettingsCommand>(
            "dbSettingsButton",
            "Database Settings",
            "Open database settings",
            "/RevitTemplate;component/Resources/Icons/RibbonIcon16.png"
        );
        var exportSettingsButton =
            CreatePushButtonData<OpenExportSettingsCommand>(
                "exportSettingsButton",
                "Export Settings",
                "Open export settings",
                "/RevitTemplate;component/Resources/Icons/RibbonIcon16.png"
            );
        settingsPanel.AddStackedItems(dbSettingsButton, exportSettingsButton);

        var commandsPanel = Application.CreatePanel("Commands", "ASRR");
        commandsPanel.AddPushButton<PlaceWallCommand>("Place Wall").ToolTip = "Place wall";
        commandsPanel.AddPushButton<ConfigureFacadeCommand>("Configure Facade").ToolTip = "Configure facade by id";
    }

    private static PushButtonData CreatePushButtonData<T>(string name, string text, string toolTip = null,
        string imagePath = null, string largeImagePath = null)
    {
        var type = typeof(T);
        var fullName = type.FullName;
        return new PushButtonData(
            name,
            text,
            Uri.UnescapeDataString(new UriBuilder(Assembly.GetAssembly(type).CodeBase).Path),
            fullName
        )
        {
            ToolTip = toolTip,
            Image = imagePath != null ? new BitmapImage(new Uri(imagePath, UriKind.Relative)) : null,
            LargeImage = largeImagePath != null ? new BitmapImage(new Uri(largeImagePath, UriKind.Relative)) : null
        };
    }
}