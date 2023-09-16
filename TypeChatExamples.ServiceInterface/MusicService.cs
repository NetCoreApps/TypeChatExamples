using ServiceStack;
using ServiceStack.AI;
using ServiceStack.Text;
using SpotifyAPI.Web;
using TypeChatExamples.ServiceModel;

namespace TypeChatExamples.ServiceInterface;

public class MusicService : Service
{
    public async Task<object> Post(CreateSpotifyChat request)
    {
        var chat = await Gateway.SendAsync(new CreateChat
        {
            Feature = Tags.Music, 
            UserMessage = request.UserMessage, 
            Translator = TypeChatTranslator.Program,
        });

        var programRequest = chat.ChatResponse.FromJson<TypeChatProgramResponse>();
        var spotifySession = (await GetSessionAsync()).GetAuthTokens("spotify");
        if (spotifySession != null)
        {
            try
            {
                var programResult = await BindAndRun<SpotifyProgram>(programRequest, new Dictionary<string, string>
                {
                    ["SpotifyToken"] = spotifySession.AccessToken
                });
                return X.Map(programResult.RunDetails, r => new CreateSpotifyChatResponse
                {
                    StepResults = r.StepResults,
                    Result = r.Result,
                    Steps = r.Steps,
                })!;
            }
            catch (APIException e)
            {
                if (!e.Message.Contains("premium required", StringComparison.OrdinalIgnoreCase))
                    throw;
            }
        }
        
        return new CreateSpotifyChatResponse
        {
            Steps = programRequest.Steps 
        };
    }
    
    private async Task<SpotifyProgramBase> BindAndRun<T>(TypeChatProgramResponse mathResult, Dictionary<string, string>? config = null) 
        where T : SpotifyProgramBase, new() 
    {
        var prog = new T { Config = config ?? new Dictionary<string, string>() };
        prog.Init();

        var steps = mathResult.Steps;
        prog.RunDetails.Steps = new List<TypeChatStep>();
        foreach (var step in steps)
        {
            var result = await ProcessStep(step, prog);
            prog.RunDetails.StepResults.Add(result);
        }

        return prog;
    }
    
    private async Task<object> ProcessStep<T>(TypeChatStep step, T prog) where T : SpotifyProgramBase, new()
    {
        var func = step.Func;
        var args = step.Args ?? new();
        var method = typeof(T).GetMethod(func);

        if (method == null)
            throw new NotSupportedException($"Unsupported func: {func}");

        var methodParams = method.GetParameters();
        var paramValues = new object[methodParams.Length];
        
        for (int i = 0; i < args.Count; i++)
        {
            var param = methodParams[i];
            var arg = args[i];

            if (arg is Dictionary<string, object> dict)
            {
                // Handle reference or nested function
                if (dict.TryGetValue("@ref", out var refVal))
                {
                    paramValues[i] = prog.RunDetails.StepResults[(int)refVal];
                    continue;
                }

                if (dict.TryGetValue("@func", out var funcVal))
                {
                    var innerStep = dict.ToJson().FromJson<TypeChatStep>();
                    paramValues[i] = ProcessStep<T>(innerStep, prog);
                    continue;
                }
            }

            if (param.ParameterType == typeof(String) || param.ParameterType is { IsValueType: true, IsEnum: false })
            {
                // For value types, use Convert.ChangeType
                paramValues[i] = Convert.ChangeType(arg, Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType);
            }
            else
            {
                // For reference types, deserialize from JSON
                string jsonArg = arg.ToJson();
                paramValues[i] = JsonSerializer.DeserializeFromString(jsonArg, param.ParameterType);
            }
        }

        object? result = null;
        if (typeof(Task).IsAssignableFrom(method.ReturnType))
        {
            // The method is async, await the result
            var task = (Task)method.Invoke(prog, paramValues)!;
            await task;

            // If the method returns a Task<T>, unwrap the result
            if (task.GetType().IsGenericType)
            {
                var resultProperty = task.GetType().GetProperty("Result");
                if (resultProperty != null)
                {
                    result = resultProperty.GetValue(task);
                }
            }
            // No else needed for Task, as result is already null
        }
        else
        {
            // For synchronous methods
            result = method.Invoke(prog, paramValues);
        }
        
        prog.RunDetails.StepResults.Add(result);
        prog.RunDetails.Steps.Add(step);

        // If the method returns a custom type, the result is already of that type
        return result;
    }
}