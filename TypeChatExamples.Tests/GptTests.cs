using TypeChatExamples.ServiceInterface;
using TypeChatExamples.ServiceModel;
using Microsoft.SemanticKernel;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.AI;
using ServiceStack.GoogleCloud;
using ServiceStack.IO;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace TypeChatExamples.Tests;

[TestFixture, Explicit, Category("Integration")]
public class GptTests
{
    IDbConnectionFactory ResolveDbFactory() => new ConfigureDb().ConfigureAndResolve<IDbConnectionFactory>();

    private ServiceStackHost CreateAppHost()
    {
        var appHost = new BasicAppHost(typeof(CoffeeShopServices).Assembly)
            {
                ConfigureAppHost = host =>
                {
                    var dbFactory = ResolveDbFactory();
                    host.VirtualFiles = new FileSystemVirtualFiles(".");
                    host.Register(dbFactory);
                    var appConfig = new AppConfig
                    {
                        CoffeeShop = new()
                        {
                            GptPath = Path.GetFullPath("gpt/coffeeshop"),
                            RecognizerId = "coffeeshop-recognizer",
                            PhraseSetId = "coffeeshop-phrases",
                        },
                        Sentiment = new()
                        {
                            GptPath = Path.GetFullPath("gpt/sentiment"),
                        }
                    };
                    host.Register(appConfig);
                    host.Register(new GoogleCloudConfig {
                        Project = "servicestackdemo",
                        Location = "global",
                        Bucket = "servicestack-typechat",
                    });
                    
                    host.LoadPlugin(new AutoQueryFeature());
                    
                    var kernel = Kernel.CreateBuilder()
                        .AddOpenAIChatCompletion(
                            Environment.GetEnvironmentVariable("OPENAI_MODEL")!,
                            Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)
                        .Build();

                    host.Register(kernel);
                    var services = host.Container;
                    services.AddSingleton<ITypeChat>(c => new KernelTypeChat(c.Resolve<Kernel>()));
                    services.AddSingleton<CoffeeShopPromptProvider>();
                    services.AddSingleton<SentimentPromptProvider>();
                    services.AddSingleton<CalendarPromptProvider>();
                    services.AddSingleton<RestaurantPromptProvider>();
                    services.AddSingleton<MathPromptProvider>();
                    services.AddSingleton<MusicPromptProvider>();
                    services.AddSingleton<IPromptProviderFactory>(c => new PromptProviderFactory {
                        Providers = {
                            [Tags.CoffeeShop] = c.Resolve<CoffeeShopPromptProvider>(),
                            [Tags.Sentiment] = c.Resolve<SentimentPromptProvider>(),
                            [Tags.Calendar] = c.Resolve<CalendarPromptProvider>(),
                            [Tags.Restaurant] = c.Resolve<RestaurantPromptProvider>(),
                            [Tags.Math] = c.Resolve<MathPromptProvider>(),
                            [Tags.Music] = c.Resolve<MusicPromptProvider>(),
                        }
                    });
                    
                }
            }
            .Init();
        return appHost;
    }

    [Test]
    public void Dump_Categories()
    {
        var dbFactory = ResolveDbFactory();
        using var db = dbFactory.Open();
    
        db.LoadSelect(db.From<Category>()).PrintDump();
    }

    [Test]
    public async Task Dump_all_phrases()
    {
        using var appHost = CreateAppHost();
        var service = appHost.Resolve<GptServices>();
        var response = await service.Any(new GetPhrases {
            Feature = Tags.CoffeeShop
        });
        
        response.Results.PrintDump();
    }

    [Test]
    public async Task Execute_semantic_kernel_prompt()
    {
        using var appHost = CreateAppHost();
        var service = appHost.Resolve<SentimentPromptProvider>();
        var prompt = await service.CreatePromptAsync("i wanna latte macchiato with vanilla");
        prompt.Print();
        var typeChat = appHost.Resolve<ITypeChat>();
        var response = await typeChat.TranslateMessageAsync(new TypeChatRequest(null, prompt, null));
        
        "Response:".Print();
        response.PrintDump();
    }

    [Test]
    public async Task Execute_Raw_Prompt()
    {
        var request = "i wanna latte macchiato with vanilla";
        //var json = await File.ReadAllTextAsync("../../../request01.json");
        //json.Print();

        using var appHost = CreateAppHost();
        var service = appHost.Resolve<GptServices>();
        var prompt = await service.Any(new GetPrompt
        {
            Feature = Tags.CoffeeShop,
            UserMessage = request +
                          @"
JSON validation failed: 'vanilla' is not a valid name for the type: Syrups
``` 
export interface Syrups {
    type: 'Syrups';
    name: 'almond syrup' | 'buttered rum syrup' | 'caramel syrup' | 'cinnamon syrup' | 'hazelnut syrup' | 
        'orange syrup' | 'peppermint syrup' | 'raspberry syrup' | 'toffee syrup' | 'vanilla syrup';
    optionQuantity?: OptionQuantity;
}
```
",
        });
        
        var dto = new Dictionary<string, object>
        {
            ["model"] = "gpt-3.5-turbo",
            ["messages"] = new List<object> {
                new Dictionary<string,object>
                {
                    ["role"] = "user",
                    ["content"] = prompt,
                }
            },
            ["temperature"] = 0,
            ["n"] = 1
        };
        var json = JSON.stringify(dto);
        //json.Print();
        
        var response = await "https://api.openai.com/v1/chat/completions"
            .PostJsonToUrlAsync(json, requestFilter: req =>
            {
                req.With(x =>
                {
                    x.SetAuthBearer(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!);
                    x.ContentType = MimeTypes.Json;
                    x.Accept = MimeTypes.Json;
                });
            });
        
        response.Print();
    }
    
    [Test]
    public async Task Execute_Prompt()
    {
        var request = "i'd like a latte that's it";
        using var appHost = CreateAppHost();

        var service = appHost.Resolve<GptServices>();
        service.Request = new MockHttpRequest();
        // var schema = (string) await service.Any(new CoffeeShopSchema());
        // schema.Print();
        var prompt = await service.Any(new CreateChat
        {
            Feature = Tags.CoffeeShop,
            UserMessage = request,
        });
        prompt.ChatResponse.Print();
    }
}