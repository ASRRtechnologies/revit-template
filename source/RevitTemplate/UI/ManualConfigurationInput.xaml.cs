using System.Windows;
using Autodesk.Revit.UI;

namespace RevitTemplate.UI
{
    /// <summary>
    /// Interaction logic for ManualConfigurationInput.xaml
    /// </summary>
    public partial class ManualConfigurationInput : Window
    {
        public string Id { get; set; }

        public ManualConfigurationInput()
        {
            InitializeComponent();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            Id = IdInputBox.Text;
            if (string.IsNullOrWhiteSpace(Id))
            {
                var taskDialog = new TaskDialog("Error")
                {
                    MainInstruction = "Please input an id"
                };
                var result = taskDialog.Show();
                if (result is TaskDialogResult.Cancel or TaskDialogResult.Close) Activate();
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}