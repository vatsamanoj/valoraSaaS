namespace Valora.Api.Domain.Common;

public interface IAggregateRoot
{
    // Marker interface
    uint Version { get; set; }
}
