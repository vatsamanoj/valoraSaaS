namespace Valora.Api.Application.Schemas.TemplateConfig;

public class CloudStorageConfig
{
    public List<StorageProviderConfig> Providers { get; set; } = new();
    public GlobalStorageSettings GlobalSettings { get; set; } = new();
}

public class StorageProviderConfig
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public ProviderConfig Config { get; set; } = new();
    public EncryptedCredentials Credentials { get; set; } = new();
    public LifecycleRules? LifecycleRules { get; set; }
}

public class ProviderConfig
{
    // AWS S3
    public string? BucketName { get; set; }
    public string? Region { get; set; }

    // Azure Blob
    public string? AccountName { get; set; }
    public string? ContainerName { get; set; }

    // GCP Storage
    public string? ProjectId { get; set; }

    // Common
    public string BasePath { get; set; } = string.Empty;
    public string? Encryption { get; set; }
}

public class EncryptedCredentials
{
    // AWS
    public string? AccessKeyId { get; set; }
    public string? SecretAccessKey { get; set; }
    public string? SessionToken { get; set; }

    // Azure
    public string? ConnectionString { get; set; }

    // GCP
    public string? ServiceAccountKey { get; set; }
}

public class LifecycleRules
{
    public int? AutoDeleteAfterDays { get; set; }
    public int? MoveToColdStorageAfterDays { get; set; }
}

public class GlobalStorageSettings
{
    public bool VirusScanEnabled { get; set; } = true;
    public bool GenerateThumbnails { get; set; } = true;
    public List<string> ThumbnailSizes { get; set; } = new() { "100x100", "300x300" };
    public List<string> AllowedMimeTypes { get; set; } = new();
    public int MaxFileSizeMB { get; set; } = 100;
}
