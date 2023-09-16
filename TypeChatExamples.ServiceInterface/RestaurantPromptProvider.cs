using ServiceStack;
using ServiceStack.Data;
using ServiceStack.AI;
using ServiceStack.Script;

namespace TypeChatExamples.ServiceInterface;

public class RestaurantPromptProvider : IPromptProvider
{
    public IDbConnectionFactory DbFactory { get; set; }
    public AppConfig Config { get; set; }

    public RestaurantPromptProvider(IDbConnectionFactory dbFactory, AppConfig config)
    {
        DbFactory = dbFactory;
        Config = config;
    }
    
    public async Task<string> CreateSchemaAsync(CancellationToken token = default)
    {
        var file = new FileInfo(Config.Restaurant.GptPath.CombineWith("schema.ss"));
        if (file == null)
            throw HttpError.NotFound($"{Config.Restaurant.GptPath}/schema.ss not found");
        
        var tpl = await file.ReadAllTextAsync(token: token);
        var context = new ScriptContext {
            Plugins = { new TypeScriptPlugin() }
        }.Init();

        var output = await new PageResult(context.OneTimePage(tpl))
        {
            Args = new Dictionary<string, object>(),
        }.RenderScriptAsync(token: token);
        return output;
    }

    public async Task<string> CreatePromptAsync(string userMessage, CancellationToken token = default)
    {
        var file = new FileInfo(Config.Restaurant.GptPath.CombineWith("prompt.ss"));
        if (file == null)
            throw HttpError.NotFound($"{Config.Restaurant.GptPath}/prompt.ss not found");
        
        var schema = await CreateSchemaAsync(token:token);
        var tpl = await file.ReadAllTextAsync(token: token);
        var context = new ScriptContext {
            Plugins = { new TypeScriptPlugin() }
        }.Init();

        var prompt = await new PageResult(context.OneTimePage(tpl))
        {
            Args =
            {
                [nameof(schema)] = schema,
                [nameof(userMessage)] = userMessage,
            }
        }.RenderScriptAsync(token: token);

        return prompt;
    }
}