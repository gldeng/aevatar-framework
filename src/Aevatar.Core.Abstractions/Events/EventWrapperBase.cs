// ReSharper disable once CheckNamespace
namespace Aevatar.Core.Abstractions;

using System.Collections.Generic;

[GenerateSerializer]
public abstract class EventWrapperBase
{
    [Id(0)]
    public Dictionary<string, string> ContextMetadata { get; set; } = new Dictionary<string, string>();
}