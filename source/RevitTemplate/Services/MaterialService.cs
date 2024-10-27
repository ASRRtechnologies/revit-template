using ASRR.Revit.Core.Warnings;
using Autodesk.Revit.DB.Visual;
using RevitTemplate.Dto;
using RevitTemplate.Exceptions;
using RevitTemplate.Model;

namespace RevitTemplate.Services;

public class MaterialService
{
    public void CreateMaterial(Document doc, MaterialDetails materialDetails)
    {
        var materials = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Materials)
            .OfClass(typeof(Material))
            .ToElements()
            .Cast<Material>();

        if (materials.Any(m => m.Name == materialDetails.Name)) return;

        using var transaction = WarningDiscardFailuresPreprocessor.GetTransaction(doc);
        transaction.Start("Create material");

        try
        {
            var newMaterialId = Material.Create(doc, materialDetails.Name);
            if (doc.GetElement(newMaterialId) is not Material newMaterial)
            {
                throw new ConfigurationFailedException("Failed to create new material");
            }

            var appearanceAssets = new FilteredElementCollector(doc)
                .OfClass(typeof(AppearanceAssetElement));

            if (appearanceAssets.FirstElement() is not AppearanceAssetElement templateAsset)
            {
                throw new ConfigurationFailedException(
                    "No appearance assets found in document. Make sure template contains a material with an appearance asset");
            }

            // var templateAsset = AppearanceAssetElement.GetAppearanceAssetElementByName(doc, "Basket Weave");
            var newAsset = templateAsset.Duplicate($"{materialDetails.Name}_AppearanceAsset");
            newMaterial.AppearanceAssetId = newAsset.Id;

            using (var scope = new AppearanceAssetEditScope(doc))
            {
                var editableAsset = scope.Start(newAsset.Id);
                try
                {
                    foreach (var texture in materialDetails.Textures)
                    {
                        var assetPropertyKey = GetAssetPropertyKey(texture.Key);
                        if (assetPropertyKey == null) continue;

                        var assetProperty = editableAsset.FindByName(assetPropertyKey);
                        if (assetProperty == null)
                        {
                            throw new ConfigurationFailedException(
                                $"Appearance asset does not contain the required property '{assetPropertyKey}'");
                        }

                        var connectedAsset = assetProperty.GetSingleConnectedAsset();

                        if (connectedAsset == null)
                        {
                            // Add a new default connected asset
                            assetProperty.AddConnectedAsset("UnifiedBitmap");
                            connectedAsset = assetProperty.GetSingleConnectedAsset();
                        }

                        if (connectedAsset.FindByName(UnifiedBitmap.UnifiedbitmapBitmap) is not AssetPropertyString
                            bitmapPath)
                        {
                            throw new ConfigurationFailedException("Failed to create new material texture");
                        }

                        if (bitmapPath.IsValidValue(texture.Value))
                        {
                            bitmapPath.Value = texture.Value;
                        }
                    }

                    scope.Commit(true);
                }
                catch (Exception)
                {
                    scope.Cancel();
                    throw;
                }
            }

            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.RollBack();
            throw;
        }
    }

    private static string GetAssetPropertyKey(TextureType textureType)
    {
        return textureType switch
        {
            TextureType.RvtPng => Generic.GenericDiffuse,
            TextureType.Bump => Generic.GenericBumpMap,
            _ => null
        };
    }
}