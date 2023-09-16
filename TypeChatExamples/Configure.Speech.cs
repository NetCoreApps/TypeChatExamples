﻿using TypeChatExamples.ServiceInterface;
using TypeChatExamples.ServiceModel;
using ServiceStack.AI;
using ServiceStack.IO;
using ServiceStack.GoogleCloud;
using Google.Cloud.Speech.V2;

[assembly: HostingStartup(typeof(TypeChatExamples.ConfigureSpeech))]

namespace TypeChatExamples;

public class ConfigureSpeech : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) =>
        {
            if (AppTasks.IsRunAsAppTask()) return;

            var speechProvider = context.Configuration.GetValue<string>("SpeechProvider");
            if (speechProvider == nameof(GoogleCloudSpeechToText))
            {
                AppHost.AssertGoogleCloudCredentials();
                services.AddSingleton<ISpeechToText>(c => new GoogleCloudSpeechToText(
                    X.Map(c.Resolve<AppConfig>(), config =>
                    {
                        var siteConfig = config.GetSiteConfig(Tags.CoffeeShop);
                        return new GoogleCloudSpeechConfig
                        {
                            Project = config.Project,
                            Location = config.Location,
                            Bucket = siteConfig.Bucket,
                            RecognizerId = siteConfig.RecognizerId,
                            PhraseSetId = siteConfig.PhraseSetId,
                        };
                    })!, SpeechClient.Create()));
            }
            else if (speechProvider == nameof(WhisperApiSpeechToText))
            {
                services.AddSingleton<ISpeechToText, WhisperApiSpeechToText>();
            }
            else if (speechProvider == nameof(WhisperLocalSpeechToText))
            {
                services.AddSingleton<ISpeechToText>(c => new WhisperLocalSpeechToText {
                    WhisperPath = c.Resolve<AppConfig>().WhisperPath ?? ProcessUtils.FindExePath("whisper"),
                    TimeoutMs = c.Resolve<AppConfig>().NodeProcessTimeoutMs,
                });
            }
            else throw new NotSupportedException($"Unknown SpeechProvider '{speechProvider}'");
        })
        .ConfigureAppHost(appHost => {
            if (AppTasks.IsRunAsAppTask()) return;

            if (appHost.Resolve<ISpeechToText>() is IRequireVirtualFiles requireVirtualFiles)
            {
                requireVirtualFiles.VirtualFiles = appHost.VirtualFiles;
            }
        });
}