using System.Text.Json;
using DesktopPets.App.Models;
using DesktopPets.App.Services;

namespace DesktopPets.Tests;

public sealed class PetAssetServiceTests
{
    [Fact]
    public void ImportImageCopiesSourceAndCreatesManifest()
    {
        using var workspace = new TempWorkspace();
        var sourcePath = System.IO.Path.Combine(workspace.Path, "source.png");
        File.WriteAllBytes(sourcePath, [1, 2, 3, 4]);

        var service = new PetAssetService(new AppPaths(System.IO.Path.Combine(workspace.Path, "data")));
        var result = service.ImportImage(sourcePath);

        Assert.True(File.Exists(result.ManifestPath));
        Assert.True(File.Exists(result.IdleImagePath));
        Assert.Contains("pets", result.ManifestPath);

        var manifest = JsonSerializer.Deserialize<PetManifest>(
            File.ReadAllText(result.ManifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(manifest);
        Assert.Equal(result.PetId, manifest!.Id);
        Assert.Equal("source/input.png", manifest.SourceImage);
        Assert.True(manifest.States.ContainsKey("idle"));
        Assert.Equal("sprites/idle.png", manifest.States["idle"].Image);
    }

    [Fact]
    public void ImportImageRejectsUnsupportedExtensions()
    {
        using var workspace = new TempWorkspace();
        var sourcePath = System.IO.Path.Combine(workspace.Path, "notes.txt");
        File.WriteAllText(sourcePath, "not an image");

        var service = new PetAssetService(new AppPaths(System.IO.Path.Combine(workspace.Path, "data")));

        Assert.Throws<NotSupportedException>(() => service.ImportImage(sourcePath));
    }

    [Fact]
    public void ImportDroppedPathCopiesCodexPetPackage()
    {
        using var workspace = new TempWorkspace();
        var sourceDirectory = System.IO.Path.Combine(workspace.Path, "guagua-cat");
        Directory.CreateDirectory(sourceDirectory);
        File.WriteAllText(
            System.IO.Path.Combine(sourceDirectory, "pet.json"),
            """
            {
              "id": "guagua-cat",
              "displayName": "Guagua Cat",
              "description": "Test pet",
              "spritesheetPath": "spritesheet.webp"
            }
            """);
        File.WriteAllBytes(System.IO.Path.Combine(sourceDirectory, "spritesheet.webp"), [9, 8, 7]);

        var service = new PetAssetService(new AppPaths(System.IO.Path.Combine(workspace.Path, "data")));
        var asset = service.ImportDroppedPath(sourceDirectory);

        Assert.Equal(PetAssetKind.CodexSpriteSheet, asset.Kind);
        Assert.Equal("Guagua Cat", asset.DisplayName);
        Assert.True(File.Exists(asset.ManifestPath));
        Assert.True(File.Exists(asset.ImagePath));
        Assert.EndsWith("spritesheet.webp", asset.ImagePath);
    }

    [Fact]
    public void ImportDroppedPathTreatsReferencedSpritesheetAsCodexPet()
    {
        using var workspace = new TempWorkspace();
        var sourceDirectory = System.IO.Path.Combine(workspace.Path, "guagua-cat");
        Directory.CreateDirectory(sourceDirectory);
        var spritesheetPath = System.IO.Path.Combine(sourceDirectory, "spritesheet.webp");
        File.WriteAllText(
            System.IO.Path.Combine(sourceDirectory, "pet.json"),
            """
            {
              "id": "guagua-cat",
              "displayName": "Guagua Cat",
              "description": "Test pet",
              "spritesheetPath": "spritesheet.webp"
            }
            """);
        File.WriteAllBytes(spritesheetPath, [9, 8, 7]);

        var service = new PetAssetService(new AppPaths(System.IO.Path.Combine(workspace.Path, "data")));
        var asset = service.ImportDroppedPath(spritesheetPath);

        Assert.Equal(PetAssetKind.CodexSpriteSheet, asset.Kind);
    }

    [Fact]
    public void ImportDroppedPathsAcceptsManifestAndSpritesheetPair()
    {
        using var workspace = new TempWorkspace();
        var manifestDirectory = System.IO.Path.Combine(workspace.Path, "manifest");
        var spritesheetDirectory = System.IO.Path.Combine(workspace.Path, "sprites");
        Directory.CreateDirectory(manifestDirectory);
        Directory.CreateDirectory(spritesheetDirectory);

        var manifestPath = System.IO.Path.Combine(manifestDirectory, "pet.json");
        var spritesheetPath = System.IO.Path.Combine(spritesheetDirectory, "spritesheet.webp");
        File.WriteAllText(
            manifestPath,
            """
            {
              "id": "paired-cat",
              "displayName": "Paired Cat",
              "description": "Test paired pet",
              "spritesheetPath": "spritesheet.webp"
            }
            """);
        File.WriteAllBytes(spritesheetPath, [5, 4, 3]);

        var service = new PetAssetService(new AppPaths(System.IO.Path.Combine(workspace.Path, "data")));
        var asset = service.ImportDroppedPaths([manifestPath, spritesheetPath]);

        Assert.Equal(PetAssetKind.CodexSpriteSheet, asset.Kind);
        Assert.Equal("Paired Cat", asset.DisplayName);
        Assert.True(File.Exists(asset.ManifestPath));
        Assert.True(File.Exists(asset.ImagePath));

        var copiedManifest = JsonSerializer.Deserialize<CodexPetManifest>(
            File.ReadAllText(asset.ManifestPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(copiedManifest);
        Assert.Equal("spritesheet.webp", copiedManifest!.SpritesheetPath);
    }
}
