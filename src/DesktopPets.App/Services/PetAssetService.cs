using System.IO;
using System.Text.Json;
using DesktopPets.App.Models;

namespace DesktopPets.App.Services;

public sealed record PetImportResult(
    string PetId,
    string ManifestPath,
    string IdleImagePath,
    PetManifest Manifest);

public sealed class PetAssetService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly AppPaths _paths;

    public PetAssetService(AppPaths paths)
    {
        _paths = paths;
    }

    public static bool IsSupportedImage(string filePath)
    {
        return SupportedExtensions.Contains(Path.GetExtension(filePath));
    }

    public static bool IsSupportedDropPath(string path)
    {
        return IsSupportedDropPaths([path]);
    }

    public static bool IsSupportedDropPaths(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return false;
        }

        if (TryFindCodexManifest(paths, out var manifestPath) &&
            TryFindCodexSpritesheet(paths, manifestPath, out _))
        {
            return true;
        }

        return paths.Count == 1 && IsSupportedSingleDropPath(paths[0]);
    }

    private static bool IsSupportedSingleDropPath(string path)
    {
        if (Directory.Exists(path))
        {
            return File.Exists(Path.Combine(path, "pet.json"));
        }

        if (!File.Exists(path))
        {
            return false;
        }

        return IsSupportedImage(path) ||
               string.Equals(Path.GetFileName(path), "pet.json", StringComparison.OrdinalIgnoreCase);
    }

    public PetAsset ImportDroppedPath(string sourcePath)
    {
        return ImportDroppedPaths([sourcePath]);
    }

    public PetAsset ImportDroppedPaths(IReadOnlyList<string> sourcePaths)
    {
        if (sourcePaths.Count == 0)
        {
            throw new ArgumentException("没有拖入任何文件。", nameof(sourcePaths));
        }

        if (TryFindCodexManifest(sourcePaths, out var manifestPath))
        {
            if (!TryFindCodexSpritesheet(sourcePaths, manifestPath, out var spritesheetPath))
            {
                throw new FileNotFoundException("没有找到 Codex 宠物的 spritesheet 文件。");
            }

            return ImportCodexPetManifest(manifestPath, spritesheetPath);
        }

        if (sourcePaths.Count == 1)
        {
            return ImportSingleDroppedPath(sourcePaths[0]);
        }

        throw new NotSupportedException("请同时拖入 Codex 宠物的 pet.json 和 spritesheet 文件。");
    }

    private PetAsset ImportSingleDroppedPath(string sourcePath)
    {
        if (Directory.Exists(sourcePath))
        {
            return ImportCodexPetManifest(Path.Combine(sourcePath, "pet.json"), spritesheetOverridePath: null);
        }

        if (string.Equals(Path.GetFileName(sourcePath), "pet.json", StringComparison.OrdinalIgnoreCase) &&
            IsCodexPetManifest(sourcePath))
        {
            return ImportCodexPetManifest(sourcePath, spritesheetOverridePath: null);
        }

        var siblingManifest = Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, "pet.json");
        if (File.Exists(siblingManifest) &&
            IsCodexPetManifest(siblingManifest) &&
            IsSpritesheetReferencedByManifest(siblingManifest, sourcePath))
        {
            return ImportCodexPetManifest(siblingManifest, sourcePath);
        }

        var result = ImportImage(sourcePath);
        return new PetAsset(result.ManifestPath, result.Manifest.Name, PetAssetKind.SingleImage, result.IdleImagePath);
    }

    public PetImportResult ImportImage(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("图片路径为空。", nameof(sourcePath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("没有找到图片文件。", sourcePath);
        }

        var extension = Path.GetExtension(sourcePath);
        if (!SupportedExtensions.Contains(extension))
        {
            throw new NotSupportedException("仅支持 PNG、JPG、JPEG 和 WEBP 图片。");
        }

        _paths.EnsureBaseDirectories();

        var petId = $"pet-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28];
        var petDirectory = Path.Combine(_paths.GeneratedPetsDirectory, petId);
        var sourceDirectory = Path.Combine(petDirectory, "source");
        var spritesDirectory = Path.Combine(petDirectory, "sprites");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(spritesDirectory);

        var normalizedExtension = extension.ToLowerInvariant();
        var sourceFileName = "input" + normalizedExtension;
        var idleFileName = "idle" + normalizedExtension;
        var copiedSourcePath = Path.Combine(sourceDirectory, sourceFileName);
        var idleImagePath = Path.Combine(spritesDirectory, idleFileName);

        File.Copy(sourcePath, copiedSourcePath, overwrite: true);
        File.Copy(copiedSourcePath, idleImagePath, overwrite: true);

        var manifest = new PetManifest
        {
            Id = petId,
            Name = Path.GetFileNameWithoutExtension(sourcePath),
            Version = 1,
            SourceImage = ToManifestPath("source", sourceFileName),
            States = new Dictionary<string, PetStateManifest>
            {
                ["idle"] = new()
                {
                    Type = "single",
                    Image = ToManifestPath("sprites", idleFileName)
                }
            }
        };

        var manifestPath = Path.Combine(petDirectory, "pet.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));

        return new PetImportResult(petId, manifestPath, idleImagePath, manifest);
    }

    public bool TryGetIdleImagePath(string manifestPath, out string idleImagePath, out string error)
    {
        idleImagePath = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            error = "没有找到宠物配置文件。";
            return false;
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<PetManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (manifest is null)
            {
                error = "宠物配置文件为空。";
                return false;
            }

            if (!manifest.States.TryGetValue("idle", out var idleState) ||
                string.IsNullOrWhiteSpace(idleState.Image))
            {
                error = "宠物配置文件没有定义待机图片。";
                return false;
            }

            var petDirectory = Path.GetDirectoryName(manifestPath) ?? string.Empty;
            var candidate = Path.GetFullPath(Path.Combine(petDirectory, idleState.Image));
            if (!File.Exists(candidate))
            {
                error = "没有找到宠物待机图片。";
                return false;
            }

            idleImagePath = candidate;
            return true;
        }
        catch (JsonException)
        {
            error = "宠物配置文件不是有效的 JSON。";
            return false;
        }
        catch (IOException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public bool TryLoadPetAsset(string manifestPath, out PetAsset? petAsset, out string error)
    {
        petAsset = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
        {
            error = "没有找到宠物配置文件。";
            return false;
        }

        if (TryReadCodexPetManifest(manifestPath, out var codexManifest, out error))
        {
            var petDirectory = Path.GetDirectoryName(manifestPath) ?? string.Empty;
            var spritesheetPath = Path.GetFullPath(Path.Combine(petDirectory, codexManifest.SpritesheetPath));
            if (!File.Exists(spritesheetPath))
            {
                error = "没有找到 Codex 宠物的 spritesheet 文件。";
                return false;
            }

            petAsset = new PetAsset(
                manifestPath,
                string.IsNullOrWhiteSpace(codexManifest.DisplayName) ? codexManifest.Id : codexManifest.DisplayName,
                PetAssetKind.CodexSpriteSheet,
                spritesheetPath);
            return true;
        }

        if (TryGetIdleImagePath(manifestPath, out var idleImagePath, out error))
        {
            petAsset = new PetAsset(manifestPath, Path.GetFileNameWithoutExtension(idleImagePath), PetAssetKind.SingleImage, idleImagePath);
            return true;
        }

        return false;
    }

    private PetAsset ImportCodexPetManifest(string manifestPath, string? spritesheetOverridePath)
    {
        if (!TryReadCodexPetManifest(manifestPath, out var manifest, out var error))
        {
            throw new NotSupportedException(error);
        }

        var sourceDirectory = Path.GetDirectoryName(manifestPath) ?? string.Empty;
        var sourceSpritesheet = spritesheetOverridePath is null
            ? Path.GetFullPath(Path.Combine(sourceDirectory, manifest.SpritesheetPath))
            : spritesheetOverridePath;
        if (!File.Exists(sourceSpritesheet))
        {
            throw new FileNotFoundException("没有找到 Codex 宠物的 spritesheet 文件。", sourceSpritesheet);
        }

        _paths.EnsureBaseDirectories();

        var safeId = MakeSafeId(string.IsNullOrWhiteSpace(manifest.Id) ? "codex-pet" : manifest.Id);
        var petDirectory = Path.Combine(_paths.GeneratedPetsDirectory, safeId);
        if (Directory.Exists(petDirectory))
        {
            petDirectory = Path.Combine(_paths.GeneratedPetsDirectory, $"{safeId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}");
        }

        Directory.CreateDirectory(petDirectory);
        var targetManifestPath = Path.Combine(petDirectory, "pet.json");
        var targetSpritesheetName = Path.GetFileName(sourceSpritesheet);
        var targetSpritesheetPath = Path.Combine(petDirectory, targetSpritesheetName);

        manifest.SpritesheetPath = targetSpritesheetName;
        File.WriteAllText(targetManifestPath, JsonSerializer.Serialize(manifest, JsonOptions));
        File.Copy(sourceSpritesheet, targetSpritesheetPath, overwrite: true);

        return new PetAsset(
            targetManifestPath,
            string.IsNullOrWhiteSpace(manifest.DisplayName) ? manifest.Id : manifest.DisplayName,
            PetAssetKind.CodexSpriteSheet,
            targetSpritesheetPath);
    }

    private static bool TryFindCodexManifest(IReadOnlyList<string> sourcePaths, out string manifestPath)
    {
        manifestPath = string.Empty;

        foreach (var sourcePath in sourcePaths)
        {
            var candidate = Directory.Exists(sourcePath)
                ? Path.Combine(sourcePath, "pet.json")
                : sourcePath;

            if (File.Exists(candidate) &&
                string.Equals(Path.GetFileName(candidate), "pet.json", StringComparison.OrdinalIgnoreCase) &&
                IsCodexPetManifest(candidate))
            {
                manifestPath = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool TryFindCodexSpritesheet(IReadOnlyList<string> sourcePaths, string manifestPath, out string spritesheetPath)
    {
        spritesheetPath = string.Empty;

        if (!TryReadCodexPetManifest(manifestPath, out var manifest, out _))
        {
            return false;
        }

        var manifestDirectory = Path.GetDirectoryName(manifestPath) ?? string.Empty;
        var referencedPath = Path.GetFullPath(Path.Combine(manifestDirectory, manifest.SpritesheetPath));
        if (File.Exists(referencedPath))
        {
            spritesheetPath = referencedPath;
            return true;
        }

        var referencedFileName = Path.GetFileName(manifest.SpritesheetPath);
        foreach (var sourcePath in sourcePaths.Where(File.Exists))
        {
            if (string.Equals(Path.GetFileName(sourcePath), referencedFileName, StringComparison.OrdinalIgnoreCase))
            {
                spritesheetPath = sourcePath;
                return true;
            }
        }

        foreach (var sourcePath in sourcePaths.Where(File.Exists))
        {
            if (string.Equals(Path.GetExtension(sourcePath), ".webp", StringComparison.OrdinalIgnoreCase))
            {
                spritesheetPath = sourcePath;
                return true;
            }
        }

        return false;
    }

    private static bool IsCodexPetManifest(string manifestPath)
    {
        return TryReadCodexPetManifest(manifestPath, out _, out _);
    }

    private static bool IsSpritesheetReferencedByManifest(string manifestPath, string spritesheetPath)
    {
        if (!TryReadCodexPetManifest(manifestPath, out var manifest, out _))
        {
            return false;
        }

        var manifestDirectory = Path.GetDirectoryName(manifestPath) ?? string.Empty;
        var referencedPath = Path.GetFullPath(Path.Combine(manifestDirectory, manifest.SpritesheetPath));
        return string.Equals(referencedPath, Path.GetFullPath(spritesheetPath), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadCodexPetManifest(string manifestPath, out CodexPetManifest manifest, out string error)
    {
        manifest = new CodexPetManifest();
        error = string.Empty;

        try
        {
            var candidate = JsonSerializer.Deserialize<CodexPetManifest>(File.ReadAllText(manifestPath), JsonOptions);
            if (candidate is null ||
                string.IsNullOrWhiteSpace(candidate.Id) ||
                string.IsNullOrWhiteSpace(candidate.SpritesheetPath))
            {
                error = "宠物配置文件不是 Codex spritesheet 宠物格式。";
                return false;
            }

            manifest = candidate;
            return true;
        }
        catch (JsonException)
        {
            error = "宠物配置文件不是有效的 JSON。";
            return false;
        }
        catch (IOException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    private static string MakeSafeId(string value)
    {
        var safeChars = value.Select(character =>
            char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-');
        return string.Concat(safeChars).Trim('-').ToLowerInvariant();
    }

    private static string ToManifestPath(params string[] parts)
    {
        return string.Join("/", parts);
    }
}
