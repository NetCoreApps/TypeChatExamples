using ServiceStack.AI;
using Microsoft.SemanticKernel;
using TypeChatExamples.ServiceInterface;
using TypeChatExamples.ServiceModel;

[assembly: HostingStartup(typeof(TypeChatExamples.ConfigureGpt))]

namespace TypeChatExamples;

public class ConfigureGpt : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) =>
        {
            services.AddSingleton<CoffeeShopPromptProvider>();
            services.AddSingleton<SentimentPromptProvider>();
            services.AddSingleton<CalendarPromptProvider>();
            services.AddSingleton<RestaurantPromptProvider>();
            services.AddSingleton<MathPromptProvider>();
            services.AddSingleton<MusicPromptProvider>();
            services.AddSingleton<IPromptProviderFactory>(c => new PromptProviderFactory {
                Providers = {
                    [Tags.CoffeeShop] = c.GetRequiredService<CoffeeShopPromptProvider>(),
                    [Tags.Sentiment] = c.GetRequiredService<SentimentPromptProvider>(),
                    [Tags.Calendar] = c.GetRequiredService<CalendarPromptProvider>(),
                    [Tags.Restaurant] = c.GetRequiredService<RestaurantPromptProvider>(),
                    [Tags.Math] = c.GetRequiredService<MathPromptProvider>(),
                    [Tags.Music] = c.GetRequiredService<MusicPromptProvider>(),
                }
            });
            
            // Call Open AI Chat API directly without going through node TypeChat
            var gptProvider = context.Configuration.GetValue<string>("TypeChatProvider");
            if (gptProvider == nameof(KernelTypeChat))
            {
                var kernel = new KernelBuilder().WithOpenAIChatCompletionService(
                        Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-3.5-turbo", 
                        Environment.GetEnvironmentVariable("OPENAI_API_KEY")!)
                    .Build();
                services.AddSingleton(kernel);
                services.AddSingleton<ITypeChat>(c => new KernelTypeChat(c.GetRequiredService<IKernel>()));
            }
            else if (gptProvider == nameof(NodeTypeChat))
            {
                // Call Open AI Chat API through node TypeChat
                services.AddSingleton<ITypeChat>(c => new NodeTypeChat());
            }
            else throw new NotSupportedException($"Unknown TypeChat Provider: {gptProvider}");
        });
}
