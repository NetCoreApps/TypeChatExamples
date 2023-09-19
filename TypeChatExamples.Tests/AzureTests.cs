using Amazon.S3;
using NUnit.Framework;
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


}