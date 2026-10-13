namespace DesktopPets.App.Models;

public enum PetAssetKind
{
    SingleImage,
    CodexSpriteSheet
}

public sealed record PetAsset(
    string ManifestPath,
    string DisplayName,
    PetAssetKind Kind,
    string ImagePath);
