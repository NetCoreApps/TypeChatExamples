﻿using ServiceStack.AI;
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
                    [Tags.CoffeeShop] = c.Resolve<CoffeeShopPromptProvider>(),
                    [Tags.Sentiment] = c.Resolve<SentimentPromptProvider>(),
                    [Tags.Calendar] = c.Resolve<CalendarPromptProvider>(),
                    [Tags.Restaurant] = c.Resolve<RestaurantPromptProvider>(),
                    [Tags.Math] = c.Resolve<MathPromptProvider>(),
                    [Tags.Music] = c.Resolve<MusicPromptProvider>(),
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
                services.AddSingleton<ITypeChat>(c => new KernelTypeChat(c.Resolve<IKernel>()));
            }
            else if (gptProvider == nameof(NodeTypeChat))
            {
                // Call Open AI Chat API through node TypeChat
                services.AddSingleton<ITypeChat>(c => new NodeTypeChat());
            }
            else throw new NotSupportedException($"Unknown TypeChat Provider: {gptProvider}");
        });
}
