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
        var arguments = Environment.GetCommandLineArgs();
        if (arguments.Contains("-workermode"))
        {
            // TODO: start worker mode automatically
        }
        else
        {
            Host.Start();
            CreateRibbon();
        }
    }

    private void CreateAltRibbon()
    {
        SetupTestPanel();
    }

    private void CreateRibbon()
    {
        SetupTestPanel();
        SetupSettingsPanel();
        SetupCommandsPanel();
    }

    private void SetupTestPanel()
    {
        var testPanel = Application.CreatePanel("Test", "ASRR");
        testPanel.AddPushButton<PopupCommand>("Click Me!")
            .SetImage("/RevitTemplate;component/Resources/Icons/RibbonIcon16.png")
            .SetLargeImage("/RevitTemplate;component/Resources/Icons/RibbonIcon32.png");
    }

    private void SetupSettingsPanel()
    {
        var settingsPanel = Application.CreatePanel("Settings", "ASRR");
        var dbSettingsButton = CreatePushButtonData<OpenDatabaseSettingsCommand>(
            "dbSettingsButton",
            "Database Settings",
            "Open database settings",
            "/RevitTemplate;component/Resources/Icons/DbSettingsIcon16.png",
            "/RevitTemplate;component/Resources/Icons/DbSettingsIcon32.png"
        );
        var exportSettingsButton =
            CreatePushButtonData<OpenExportSettingsCommand>(
                "exportSettingsButton",
                "Export Settings",
                "Open export settings",
                "/RevitTemplate;component/Resources/Icons/ExportSettingsIcon16.png",
                "/RevitTemplate;component/Resources/Icons/ExportSettingsIcon32.png"
            );
        settingsPanel.AddStackedItems(dbSettingsButton, exportSettingsButton);
    }

    private void SetupCommandsPanel()
    {
        var commandsPanel = Application.CreatePanel("Commands", "ASRR");
        // commandsPanel.AddPushButton<PlaceWallCommand>("Place Wall").ToolTip = "Place wall";
        commandsPanel.AddPushButton<ConfigureFacadeCommand>("Configure\r\nFacade")
            .SetImage("/RevitTemplate;component/Resources/Icons/FacadeIcon16.png")
            .SetLargeImage("/RevitTemplate;component/Resources/Icons/FacadeIcon32.png")
            .ToolTip = "Configure facade by id";

        commandsPanel.AddPushButton<ConfigureProjectCommand>("Configure\r\nProject")
            .SetImage("/RevitTemplate;component/Resources/Icons/ProjectIcon16.png")
            .SetLargeImage("/RevitTemplate;component/Resources/Icons/ProjectIcon32.png")
            .ToolTip = "Configure project by queue id";
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