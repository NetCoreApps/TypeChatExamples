using System.Runtime.Serialization;
using ServiceStack;

namespace TypeChatExamples.ServiceModel;

public class CreateMathChat : IReturn<CreateMathChatResponse>
{
    public string UserMessage { get; set; }
}

[DataContract]
public class CreateMathChatResponse
{
    [DataMember(Name = "@steps")]
    public List<TypeChatStep> Steps { get; set; }
    public ResponseStatus ResponseStatus { get; set; }
}

[DataContract]
public class TypeChatStep
{
    [DataMember(Name = "@func")]
    public string Func { get; set; }
    [DataMember(Name = "@args")]
    public List<object> Args { get; set; }
}

[DataContract]
public class TypeChatRefArg
{
    [DataMember(Name = "@ref")]
    public int Ref { get; set; }
}