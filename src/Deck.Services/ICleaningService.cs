using Deck.Core.Models;

namespace Deck.Services;

public interface ICleaningService
{
    Task<ThreeLayerCleaningResult> GetCleaningOptionsAsync();
    
    Task<ImagesCleaningResult> GetImagesCleaningStrategyAsync(int keepCount = 3, bool dryRun = false);
    
    Task<bool> ExecuteCleaningAsync(CleaningOperation operation);
    
    Task<TemplatesCleaningResult> GetTemplatesCleaningAlternativesAsync();
    
    Task<List<CleaningWarning>> GetCleaningWarningsAsync(CleaningType cleaningType, List<string> itemsToClean);
    
    Task<bool> ValidateCleaningOperationAsync(CleaningOperation operation);
}

public class ThreeLayerCleaningResult
{
    public List<CleaningOption> TemplateOptions { get; set; } = new();
    public List<CleaningOption> CustomOptions { get; set; } = new();
    public List<CleaningOption> ImageOptions { get; set; } = new();
    public CleaningRecommendation Recommendation { get; set; } = new();
}

public class ImagesCleaningResult
{
    public Dictionary<string, List<ImageInfo>> ImagesByPrefix { get; set; } = new();
    public List<ImageInfo> ImagesToKeep { get; set; } = new();
    public List<ImageInfo> ImagesToRemove { get; set; } = new();
    public int TotalToRemove { get; set; }
    public long SpaceToFree { get; set; }
    public bool IsDryRun { get; set; }
}

public class TemplatesCleaningResult
{
    public List<string> OutdatedTemplates { get; set; } = new();
    public List<string> UnusedTemplates { get; set; } = new();
    public List<CleaningAlternative> Alternatives { get; set; } = new();
}

public class CleaningOperation
{
    public CleaningType Type { get; set; }
    public List<string> ItemsToClean { get; set; } = new();
    public bool DryRun { get; set; } = false;
    public bool Force { get; set; } = false;
    public Dictionary<string, object> Options { get; set; } = new();
}


public class CleaningRecommendation
{
    public string Summary { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public CleaningStrategy Strategy { get; set; }
}

public class CleaningWarning
{
    public string Message { get; set; } = string.Empty;
    public CleaningWarningLevel Level { get; set; }
    public List<string> AffectedItems { get; set; } = new();
    public string Suggestion { get; set; } = string.Empty;
}

public class CleaningAlternative
{
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
}

