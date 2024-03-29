using Funq;
using ServiceStack.Aws;
using ServiceStack.Azure;
using ServiceStack.Configuration;
using ServiceStack.GoogleCloud;
using ServiceStack.Host;
using ServiceStack.IO;
using ServiceStack.Web;
using TypeChatExamples.ServiceModel;
using TypeChatExamples.ServiceInterface;

[assembly: HostingStartup(typeof(TypeChatExamples.AppHost))]

namespace TypeChatExamples;

public class AppHost : AppHostBase, IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context,services) => {
            // Configure ASP.NET Core IOC Dependencies
            var appConfig = context.Configuration.GetSection(nameof(AppConfig)).Get<AppConfig>();
            services.AddSingleton(appConfig);

            var gcp = context.Configuration.GetSection(nameof(GoogleCloudConfig))?.Get<GoogleCloudConfig>();
            if (gcp != null)
            {
                services.AddSingleton(gcp);
            }

            var aws = context.Configuration.GetSection(nameof(AwsConfig))?.Get<AwsConfig>();
            if (aws != null)
            {
                aws.AccountId ??= Environment.GetEnvironmentVariable("AWS_ACCOUNT_ID");
                aws.AccessKey ??= Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
                aws.SecretKey ??= Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");
                aws.Region ??= Environment.GetEnvironmentVariable("AWS_REGION");
                services.AddSingleton(aws);
            }

            var r2 = context.Configuration.GetSection(nameof(CloudflareConfig))?.Get<CloudflareConfig>();
            if (r2 != null)
            {
                r2.AccountId ??= Environment.GetEnvironmentVariable("R2_ACCOUNT_ID");
                r2.AccessKey ??= Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID");
                r2.SecretKey ??= Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY");
                services.AddSingleton(r2);
            }

            var azure = context.Configuration.GetSection(nameof(AzureConfig))?.Get<AzureConfig>();
            if (azure != null)
            {
                azure.SpeechKey ??= Environment.GetEnvironmentVariable("SPEECH_KEY");
                azure.SpeechRegion ??= Environment.GetEnvironmentVariable("SPEECH_REGION");
                azure.ConnectionString ??= Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING");
                services.AddSingleton(azure);
            }
            
            if (!AppTasks.IsRunAsAppTask())
            {
                appConfig.NodePath ??= ProcessUtils.FindExePath("node") ?? throw new Exception("Could not resolve path to node");
                appConfig.FfmpegPath ??= ProcessUtils.FindExePath("ffmpeg");
            }

            var wwwrootVfs = new FileSystemVirtualFiles(context.HostingEnvironment.ContentRootPath.CombineWith("wwwroot"));
            var vfs = new FileSystemVirtualFiles(context.HostingEnvironment.ContentRootPath);
            services.AddPlugin(new FilesUploadFeature(
                new UploadLocation("products", wwwrootVfs, allowExtensions:FileExt.WebImages,
                    resolvePath: ctx => $"/products/{ctx.FileName}"),
                new UploadLocation("recordings", vfs, allowExtensions:FileExt.WebAudios, writeAccessRole: RoleNames.AllowAnon,
                    maxFileBytes: 1024 * 1024,
                    transformFile: ctx => ConvertAudioToWebM(ctx.File),
                    resolvePath: ctx => $"/recordings/{ctx.GetDto<IRequireFeature>().Feature}/{ctx.DateSegment}/{DateTime.UtcNow.TimeOfDay.TotalMilliseconds}.{ctx.FileExtension}")
            ));
            
            services.AddPlugin(new CorsFeature(new[] {
                "http://localhost:5173", //vite dev
            }, allowCredentials:true));
            
            services.AddPlugin(new ProfilingFeature());
        });

    public AppHost() : base("TypeChat Examples", typeof(GptServices).Assembly) {}

    public override void Configure(Container container)
    {
        SetConfig(new HostConfig {
        });
    }

    /// <summary>
    /// Safari can only encode Web Audio Recordings in mp4/aac which Google speech-to-text doesn't support so we
    /// need to convert it to .webm before we send it to speech-to-text API to transcribe 
    /// </summary>
    public async Task<IHttpFile?> ConvertAudioToWebM(IHttpFile file)
    {
        if (!file.FileName.EndsWith("mp4")) 
            return file;
        
        var ffmpegPath = Container.Resolve<AppConfig>().FfmpegPath ?? ProcessUtils.FindExePath("ffmpeg") 
            ?? throw new Exception("Could not resolve path to ffmpeg");
        
        var now = DateTime.UtcNow;
        var time = $"{now:yyyy-M-d_s.fff}";
        var tmpDir = Environment.CurrentDirectory.CombineWith("App_Data/tmp").AssertDir();
        var tmpMp4 = tmpDir.CombineWith($"{time}.mp4");
        await using (File.Create(tmpMp4)) {}
        var tmpWebm = tmpDir.CombineWith($"{time}.webm");
        
        var msMp4 = await file.InputStream.CopyToNewMemoryStreamAsync();
        await using (var fsMp4 = File.OpenWrite(tmpMp4))
        {
            await msMp4.WriteToAsync(fsMp4);
        }
        await ProcessUtils.RunShellAsync($"{ffmpegPath} -i {tmpMp4} {tmpWebm}");
        File.Delete(tmpMp4);
        
        HttpFile? to = null;
        await using (var fsWebm = File.OpenRead(tmpWebm))
        {
            to = new HttpFile(file) {
                FileName = file.FileName.WithoutExtension() + ".webm",
                InputStream = await fsWebm.CopyToNewMemoryStreamAsync()
            };
        }
        File.Delete(tmpWebm);
                
        ThreadPool.QueueUserWorkItem(_ => {
            try
            {
                var origPath = $"/recordings/{now:yyyy/MM/dd}/{now.TimeOfDay.TotalMilliseconds}.mp4";
                msMp4.Position = 0;
                VirtualFiles.WriteFile(origPath, msMp4);
            }
            catch (Exception ignore) {}
        });

        return to;
    }
    
    public static void RegisterKey() =>
        Licensing.RegisterLicense("OSS GPL-3.0 2024 https://github.com/NetCoreApps/TypeChatExamples AoYtcT3/UQmzr/QOPsMfEN3HLrf4Dj2JsqOXuZAtuPoUIJhPOugkmANDQbFKRQF7wyD10d/blCPTECWwemjmKtAxg5lmLbDZ3dc8ijuZ8ACC4xGtdkCke9hbydAMXAk+O6cUYTz37C0lBsusZdv6dKV6CJQ3NsmhMZETtDzAYDg=");
}
