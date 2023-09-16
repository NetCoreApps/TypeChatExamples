using System.Runtime.Serialization;
using ServiceStack;

namespace TypeChatExamples.ServiceModel;

public class CreateSpotifyChat : IReturn<CreateSpotifyChatResponse>
{
    public string UserMessage { get; set; }
}

public class HelloResponse
{
    public string Result { get; set; } = default!;
}

public class CreateSpotifyChatResponse
{
    public List<object> StepResults { get; set; } = new();
    public object? Result { get; set; }
    [DataMember(Name = "@steps")]
    public List<TypeChatStep> Steps { get; set; } = new();
    public ResponseStatus ResponseStatus { get; set; }
}


public class TypeChatProgramResponse
{
    [DataMember(Name = "@steps")]
    public List<TypeChatStep> Steps { get; set; }
}

public class SpotifyRunDetails
{
    public List<object> StepResults { get; set; } = new();
    public object? Result { get; set; }
    public List<TypeChatStep> Steps { get; set; } = new();
}