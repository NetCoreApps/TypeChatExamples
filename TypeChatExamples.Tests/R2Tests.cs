using Amazon.S3;
using Amazon.S3.Model;
using NUnit.Framework;
using ServiceStack.Aws;
using ServiceStack.IO;
using ServiceStack.Text;

namespace TypeChatExamples.Tests;

[TestFixture, Explicit, Category("Integration")]
public class R2Tests
{
    CloudflareConfig config = new()
    {
        AccountId = Environment.GetEnvironmentVariable("R2_ACCOUNT_ID"),
        AccessKey = Environment.GetEnvironmentVariable("R2_ACCESS_KEY_ID"),
        SecretKey = Environment.GetEnvironmentVariable("R2_SECRET_ACCESS_KEY"),
        Bucket = "servicestack-coffeeshop", 
    };
    private AmazonS3Client s3Client;
    
    public R2Tests()
    {
        s3Client = new AmazonS3Client(
            config.AccessKey,
            config.SecretKey,
            new AmazonS3Config {
                ServiceURL = $"https://{config.AccountId}.r2.cloudflarestorage.com",
            });
    }

    
    [Test]
    public void Can_access_r2_bucket()
    {
        s3Client.ListObjects(new ListObjectsRequest
        {
            BucketName = config.Bucket,
        });
    }

    [Test]
    public void Can_access_r2_bucket_vfs()
    {
        var vfs = new S3VirtualFiles(s3Client, config.Bucket);

        var files = vfs.GetAllFiles().ToList();
        $"FILES {files.Count}:".Print();
        foreach (var file in files)
        {
            file.VirtualPath.Print();
        }
    }

}
