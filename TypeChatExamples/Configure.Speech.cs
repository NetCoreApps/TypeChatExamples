using Amazon;
using Amazon.TranscribeService;
using ServiceStack.AI;
using ServiceStack.IO;
using ServiceStack.GoogleCloud;
using Google.Cloud.Speech.V2;
using TypeChatExamples.ServiceInterface;

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
                GoogleCloudConfig.AssertValidCredentials();
                services.AddSingleton<ISpeechToTextFactory>(c => new SpeechToTextFactory
                {
                    Resolve = feature => new GoogleCloudSpeechToText(
                        SpeechClient.Create(),
                        X.Map(c.Resolve<AppConfig>(), config =>
                        {
                            var siteConfig = config.GetSiteConfig(feature);
                            var gcpConfig = config.AssertGcpConfig();
                            return new GoogleCloudSpeechConfig
                            {
                                Project = gcpConfig.Project,
                                Location = gcpConfig.Location,
                                Bucket = gcpConfig.Bucket,
                                RecognizerId = siteConfig.RecognizerId,
                                PhraseSetId = siteConfig.PhraseSetId,
                            };
                        })!)
                    {
                        VirtualFiles = HostContext.VirtualFiles
                    }
                });
            }
            else if (speechProvider == nameof(AwsSpeechToText))
            {
                services.AddSingleton(c => X.Map(c.Resolve<AppConfig>().AssertAwsConfig(), x => 
                    new AmazonTranscribeServiceClient(x.AccessKey, x.SecretKey, RegionEndpoint.GetBySystemName(x.Region)))!);
                services.AddSingleton<ISpeechToTextFactory>(c => new SpeechToTextFactory
                {
                    Resolve = feature => new AwsSpeechToText(
                        c.Resolve<AmazonTranscribeServiceClient>(),
                        X.Map(c.Resolve<AppConfig>(), config =>
                        {
                            var siteConfig = config.GetSiteConfig(feature);
                            var awsConfig = config.AssertAwsConfig();
                            return new AwsSpeechToTextConfig {
                                Bucket = awsConfig.Bucket,
                                VocabularyName = siteConfig.VocabularyName,
                            };
                        })!)
                    {
                        VirtualFiles = HostContext.VirtualFiles
                    }
                });
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
        .ConfigureAppHost(afterConfigure:appHost => {
            if (AppTasks.IsRunAsAppTask()) return;

            if (appHost.TryResolve<ISpeechToText>() is IRequireVirtualFiles requireVirtualFiles)
            {
                requireVirtualFiles.VirtualFiles = appHost.VirtualFiles;
            }
        });
}
