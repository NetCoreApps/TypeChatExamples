using System.Text.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.TranscribeService;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.AI;
using ServiceStack.Aws;
using ServiceStack.IO;
using ServiceStack.Text;
using TypeChatExamples.ServiceInterface;

namespace TypeChatExamples.Tests;

[Explicit, Category("Integration")]
public class AwsTests
{
    S3Config awsConfig = new()
    {
        AccountId = Environment.GetEnvironmentVariable("AWS_ACCOUNT_ID"),
        AccessKey = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"),
        SecretKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"),
        Region = Environment.GetEnvironmentVariable("AWS_REGION"),
        Bucket = "servicestack-coffeeshop", 
    };
    private AmazonS3Client s3Client;
    private IVirtualFiles VirtualFiles;
    
    public AwsTests()
    {
        s3Client = new AmazonS3Client(
            awsConfig.AccessKey,
            awsConfig.SecretKey,
            RegionEndpoint.GetBySystemName(awsConfig.Region));
        VirtualFiles = new S3VirtualFiles(s3Client, awsConfig.Bucket);
    }

    private AwsSpeechToText CreateSpeechToTextClient()
    {
        var client = new AwsSpeechToText(new AmazonTranscribeServiceClient(),
            new AwsSpeechToTextConfig
            {
                VocabularyName = "TestVocabulary",
                Bucket = awsConfig.Bucket,
            })
        {
            VirtualFiles = VirtualFiles,
        };
        return client;
    }

    [Test]
    public async Task Can_Create_Vocabulary()
    {
        var client = CreateSpeechToTextClient();
        await client.InitAsync(new InitSpeechToText
        {
            PhraseWeights = TestConfig.SimplePhrases.Map(x => KeyValuePair.Create(x, 10)), 
        });
    }
    
    [Test]
    public void Can_s3_ListBuckets()
    {
        s3Client.ListBuckets(new ListBucketsRequest());
    }

    [Test]
    public void Can_ListObjects_in_s3_bucket()
    {
        var response = s3Client.ListObjects(new ListObjectsRequest
        {
            BucketName = awsConfig.Bucket,
        });
        response.S3Objects.Map(x => x.Key).PrintDump();
    }

    [Test]
    public async Task Can_calculate_confidence()
    {
        var json = await File.ReadAllTextAsync(TestConfig.RecordingsPath + "hot-cappuccino-with-two-sugars-male.json");
        var obj = JsonDocument.Parse(json);
        var results = obj.RootElement
            .GetProperty("results");
        var transcript = results.GetProperty("transcripts")[0]
            .GetProperty("transcript");
        var confidences = results.GetProperty("items").EnumerateArray()
            .Where(x => x.GetProperty("type").ToString() == "pronunciation")
            .Map(x => x.TryGetProperty("alternatives", out var alts)
                ? (double.TryParse(alts.EnumerateArray().FirstOrDefault().GetProperty("confidence").ToString(), out var d) ? d : (double?)null)
                : null)
            .Where(x => x != null)
            .ToList();

        var avg = confidences.Count > 0
            ? Math.Round(confidences.Sum().GetValueOrDefault() / confidences.Count, 3)
            : 0;
        
        confidences.PrintDump();
        $"AVG: {avg}".Print();
    }

    [Test]
    public void Can_access_s3_bucket_vfs()
    {
        var vfs = new S3VirtualFiles(s3Client, awsConfig.Bucket);

        var files = vfs.GetAllFiles().ToList();
        $"FILES {files.Count}:".Print();
        foreach (var file in files)
        {
            file.VirtualPath.Print();
        }
    }

    [Test]
    public async Task Can_transcribe()
    {
        //await using var fs = File.OpenRead(TestConfig.RecordingsPath + TestConfig.Recordings[0]);
        var client = CreateSpeechToTextClient();

        var recordingsPath = "/recordings/" + TestConfig.Recordings[0];
        // await using var fs = File.OpenRead(TestConfig.RecordingsPath + TestConfig.Recordings[0]);
        // client.VirtualFiles.WriteFile(recordingsPath, fs);
        
        var result = await client.TranscribeAsync(recordingsPath);
        result.PrintDump();
    }
}
