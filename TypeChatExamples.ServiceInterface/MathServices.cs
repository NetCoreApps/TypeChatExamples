using ServiceStack;
using ServiceStack.AI;
using TypeChatExamples.ServiceModel;

namespace TypeChatExamples.ServiceInterface;

public class MathServices : Service
{
    public async Task<object> Post(CreateMathChat request)
    {
        var chat = await Gateway.SendAsync(new CreateChat
        {
            Feature = Tags.Math, 
            UserMessage = request.UserMessage, 
            Translator = TypeChatTranslator.Program,
        });

        var programRequest = chat.ChatResponse.FromJson<CreateMathChatResponse>();
        var programResult = BindAndRun<MathProgram>(programRequest);
        return programResult;
    }
    
    private MathProgramBase BindAndRun<T>(CreateMathChatResponse mathResult) where T : MathProgramBase,new() 
    {
        var prog = new T();
        var steps = mathResult.Steps;
        object? result = null;
        prog.Steps = new List<TypeChatStep>();
        foreach (var step in steps)
        {
            result = ProcessStep(step, prog);
            prog.StepResults.Add(result);
        }

        prog.Result = prog.StepResults.Last();

        return prog;
    }
    
    private object? ProcessStep<T>(TypeChatStep step, T prog) where T : MathProgramBase, new()
    {
        var func = step.Func;
        var args = step.Args;
        var method = typeof(T).GetMethod(func);

        if (method == null)
            throw new NotSupportedException($"Unsupported func: {func}");

        var methodParams = method.GetParameters();
        object[] paramValues = new object[methodParams.Length];

        for (int i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            var arg = args[i];

            if (arg is Dictionary<string, object> dict)
            {
                // Handle reference or nested function
                if (dict.TryGetValue("@ref", out var refVal))
                {
                    paramValues[i] = prog.StepResults[(int)refVal];
                    continue;
                }

                if (dict.TryGetValue("@func", out var funcVal))
                {
                    var innerStep = dict.ToJson().FromJson<TypeChatStep>();
                    paramValues[i] = ProcessStep(innerStep, prog);
                    continue;
                }
            }

            // Type conversion, if needed
            paramValues[i] = Convert.ChangeType(arg, param.ParameterType);
        }

        // Invoke the method
        var result = method.Invoke(prog, paramValues);

        // If the method returns void, return a default value or null
        if (method.ReturnType == typeof(void))
        {
            return null;
        }
        
        prog.StepResults.Add(result);
        prog.Steps.Add(step);

        // If the method returns a custom type, the result is already of that type
        return result;
    }
}

public class MathProgram : MathProgramBase
{
    public double add(double x, double y) => x + y;
    public double sub(double x, double y) => x - y;
    public double mul(double x, double y) => x * y;
    public double div(double x, double y) => x / y;
    public double neg(double x) => -x;
    public double id(double x) => x;
}

public class MathProgramBase
{
    public List<object> StepResults { get; set; } = new();
    public object? Result { get; set; }
    public List<TypeChatStep> Steps { get; set; } = new();
}