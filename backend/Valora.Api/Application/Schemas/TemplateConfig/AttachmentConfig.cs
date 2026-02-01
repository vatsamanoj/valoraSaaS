namespace Valora.Api.Application.Schemas.TemplateConfig;

public class AttachmentConfig
{
    public DocumentLevelAttachmentConfig DocumentLevel { get; set; } = new();
    public LineLevelAttachmentConfig LineLevel { get; set; } = new();
}

public class DocumentLevelAttachmentConfig
{
    public bool Enabled { get; set; } = false;
    public int MaxFiles { get; set; } = 10;
    public int MaxFileSizeMB { get; set; } = 50;
    public List<string> AllowedTypes { get; set; } = new();
    public List<AttachmentCategory> Categories { get; set; } = new();
    public string StorageProvider { get; set; } = "primary";
}

public class LineLevelAttachmentConfig
{
    public bool Enabled { get; set; } = false;
    public int MaxFiles { get; set; } = 3;
    public int MaxFileSizeMB { get; set; } = 10;
    public List<string> AllowedTypes { get; set; } = new();
    public List<AttachmentCategory> Categories { get; set; } = new();
    public string StorageProvider { get; set; } = "primary";
    public GridColumnConfig GridColumn { get; set; } = new();
}

public class AttachmentCategory
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
    public int? MaxFilesPerCategory { get; set; }
}

public class GridColumnConfig
{
    public string Width { get; set; } = "100px";
    public bool ShowCount { get; set; } = true;
    public bool AllowPreview { get; set; } = true;
}
