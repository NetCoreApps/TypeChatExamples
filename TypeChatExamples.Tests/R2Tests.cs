using Amazon.S3;
using Amazon.S3.Model;
using NUnit.Framework;
using ServiceStack.Aws;
using ServiceStack.IO;
using ServiceStack.Text;
using TypeChatExamples.ServiceInterface;

namespace TypeChatExamples.Tests;

public class R2Tests
{
    S3Config r2Config = new()
    {
        AccountId = Environment.GetEnvironmentVariable("R2_ACCOUNT_ID"),
        AccessKey = Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID"),
        SecretKey = Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY"),
        Region = Environment.GetEnvironmentVariable("R2_REGION"),
        Bucket = "servicestack-coffeeshop", 
    };
    private AmazonS3Client s3Client;
    
    public R2Tests()
    {
        s3Client = new AmazonS3Client(
            r2Config.AccessKey,
            r2Config.SecretKey,
            new AmazonS3Config {
                ServiceURL = $"https://{r2Config.AccountId}.r2.cloudflarestorage.com",
            });
    }

    
    [Test]
    public void Can_access_r2_bucket()
    {
        s3Client.ListObjects(new ListObjectsRequest
        {
            BucketName = r2Config.Bucket,
        });
    }

    [Test]
    public void Can_access_r2_bucket_vfs()
    {
        var vfs = new S3VirtualFiles(s3Client, r2Config.Bucket);

        var files = vfs.GetAllFiles().ToList();
        $"FILES {files.Count}:".Print();
        foreach (var file in files)
        {
            file.VirtualPath.Print();
        }
    }

}
