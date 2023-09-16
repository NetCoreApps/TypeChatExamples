﻿using ServiceStack;
using ServiceStack.AI;
using ServiceStack.Script;

namespace TypeChatExamples.ServiceInterface;

public class CalendarPromptProvider : IPromptProvider
{
    public AppConfig Config { get; set; }

    public CalendarPromptProvider(AppConfig config)
    {
        Config = config;
    }

    public async Task<string> CreateSchemaAsync(CancellationToken token = default)
    {
        var file = new FileInfo(Config.Calendar.GptPath.CombineWith("schema.ss"));
        if (file == null)
            throw HttpError.NotFound($"{Config.Calendar.GptPath}/schema.ss not found");
        
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
        var file = new FileInfo(Config.Calendar.GptPath.CombineWith("prompt.ss"));
        if (file == null)
            throw HttpError.NotFound($"{Config.Calendar.GptPath}/prompt.ss not found");
        
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