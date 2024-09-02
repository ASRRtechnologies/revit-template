using System.Windows;
using Autodesk.Revit.UI;

namespace RevitTemplate.UI
{
    /// <summary>
    /// Interaction logic for ManualConfigurationInput.xaml
    /// </summary>
    public partial class ManualFacadeConfigurationInput : Window
    {
        public string FacadeConfigurationId { get; set; }

        public ManualFacadeConfigurationInput()
        {
            InitializeComponent();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            FacadeConfigurationId = ConfigurationId.Text;
            if (string.IsNullOrWhiteSpace(FacadeConfigurationId))
            {
                var taskDialog = new TaskDialog("Error")
                {
                    MainInstruction = "Please input a facadeConfigurationId"
                };
                var result = taskDialog.Show();
                if (result == TaskDialogResult.Cancel || result == TaskDialogResult.Close) Activate();
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}