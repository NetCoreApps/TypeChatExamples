using System.Text.Json;
using Amazon.S3;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using NUnit.Framework;
using ServiceStack.AI;
using ServiceStack.Azure.Storage;
using ServiceStack.IO;
using ServiceStack.Text;
using TypeChatExamples.ServiceInterface;

namespace TypeChatExamples.Tests;

[TestFixture, Explicit, Category("Integration")]
public class AzureTests
{
    AzureConfig azureConfig = new()
    {
        ConnectionString = Environment.GetEnvironmentVariable("AZURE_BLOB_CONNECTION_STRING"),
        ContainerName = "servicestack-typechat",
        SpeechKey = Environment.GetEnvironmentVariable("SPEECH_KEY"),
        SpeechRegion = Environment.GetEnvironmentVariable("SPEECH_REGION"),
    };
    private IVirtualFiles VirtualFiles;
    
    public AzureTests()
    {
        VirtualFiles = new AzureBlobVirtualFiles(azureConfig.ConnectionString, azureConfig.ContainerName);
    }
    
    [Test]
    public void Can_access_blob_container_vfs()
    {
        var files = VirtualFiles.GetAllFiles().ToList();
        $"FILES {files.Count}:".Print();
        foreach (var file in files)
        {
            file.VirtualPath.Print();
        }
    }

    [Test]
    public void Can_download_file()
    {
        var file = VirtualFiles.GetFile("/recordings/coffeeshop/2023/09/19/33969538.2765.webm");
        Assert.That(file, Is.Not.Null);
        Assert.That(file.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task Can_transcribe_recording()
    {
        //webm needs gstreamer installed https://gstreamer.freedesktop.org/download/
        //add GSTREAMER_PATH/bin to $PATH
        var speechConfig = SpeechConfig.FromSubscription(azureConfig.SpeechKey, azureConfig.SpeechRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US";
        speechConfig.OutputFormat = OutputFormat.Detailed;
        
        var recordingPath = TestConfig.RecordingsPath + TestConfig.Recordings[0];

        using var audioInput = AudioConfig.FromStreamInput(new PullAudioInputStream(new BinaryAudioStreamReader(
                new BinaryReader(File.OpenRead(recordingPath))),
            AudioStreamFormat.GetCompressedFormat(AudioStreamContainerFormat.ANY)));

        using var recognizer = new SpeechRecognizer(speechConfig, audioInput);
        var result = await recognizer.RecognizeOnceAsync();
        var json = result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);

        Console.WriteLine($"RECOGNIZED: Text={result.Text}");
        Console.WriteLine($"json={json}");

        var confidence = default(float);
        var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("NBest", out var oNBest) && oNBest.ValueKind == JsonValueKind.Array)
        {
            var best = oNBest.EnumerateArray().FirstOrDefault();
            if (best.ValueKind == JsonValueKind.Object)
            {
                if (best.TryGetProperty("Confidence", out var oConfidence) &&
                    oConfidence.ValueKind == JsonValueKind.Number)
                {
                    confidence = oConfidence.GetSingle();
                }
            }
        }
    }

    [Test]
    public async Task Can_transcribe_recording_AzureSpeechToText()
    {
        var speechConfig = SpeechConfig.FromSubscription(azureConfig.SpeechKey, azureConfig.SpeechRegion);
        speechConfig.SpeechRecognitionLanguage = "en-US";

        var recordingPath = TestConfig.Recordings[0];
        var client = new AzureSpeechToText(speechConfig) {
            VirtualFiles = new FileSystemVirtualFiles(TestConfig.RecordingsPath)
        };
        var result = await client.TranscribeAsync(recordingPath);

        Console.WriteLine($"RECOGNIZED: Text={result.Transcript}");
        Console.WriteLine($"json={result.ApiResponse}");
    }
    
}