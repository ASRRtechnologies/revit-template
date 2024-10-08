using ASRR.Revit.Core;

namespace RevitTemplate.ModelPlacerTemp;

public class Utilities
{
    public static CopyPasteOptions UseDestinationOnDuplicateNameCopyPasteOptions()
    {
        var copyOptions = new CopyPasteOptions();
        copyOptions.SetDuplicateTypeNamesHandler(new UseDestinationHandler());
        return copyOptions;
    }
}