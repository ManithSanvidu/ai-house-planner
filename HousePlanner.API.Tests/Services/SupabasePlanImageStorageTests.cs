using System.Net;
using HousePlanner.API.Services;
using Microsoft.Extensions.Configuration;

namespace HousePlanner.API.Tests.Services;

public class SupabasePlanImageStorageTests
{
    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SUPABASE_URL"] = "https://project.supabase.co",
            ["SUPABASE_SERVICE_ROLE_KEY"] = "server-secret",
            ["SUPABASE_PLAN_IMAGE_BUCKET"] = "plan-library",
        })
        .Build();

    [Fact]
    public async Task Upload_UsesServerCredentialsAndReturnsObjectKey()
    {
        var handler = new RecordingHandler();
        var storage = new SupabasePlanImageStorage(new HttpClient(handler), Configuration());
        var planId = Guid.NewGuid();

        var key = await storage.UploadAsync(
            planId, new MemoryStream(new byte[] { 1, 2, 3 }), ".jpg", "image/jpeg");

        Assert.StartsWith($"{planId:D}/", key);
        Assert.EndsWith(".jpg", key);
        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Contains("/storage/v1/object/plan-library/", handler.Request.RequestUri!.AbsoluteUri);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("server-secret", handler.Request.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Delete_SkipsLegacyUploadPathButRemovesStorageObject()
    {
        var handler = new RecordingHandler();
        var storage = new SupabasePlanImageStorage(new HttpClient(handler), Configuration());

        await storage.DeleteAsync("/uploads/legacy.jpg");
        Assert.Null(handler.Request);

        await storage.DeleteAsync("plan-id/image.jpg");
        Assert.Equal(HttpMethod.Delete, handler.Request!.Method);
        Assert.Contains("plan-id/image.jpg", handler.Body);
    }

    [Fact]
    public void PublicUrl_PreservesAbsoluteAndLegacyReferencesAndResolvesObjectKeys()
    {
        var storage = new SupabasePlanImageStorage(new HttpClient(new RecordingHandler()), Configuration());
        const string absolute = "https://cdn.example.test/image.jpg";
        const string legacy = "/uploads/legacy.jpg";

        Assert.Equal(absolute, storage.GetPublicUrl(absolute));
        Assert.Equal(legacy, storage.GetPublicUrl(legacy));
        Assert.Equal(
            "https://project.supabase.co/storage/v1/object/public/plan-library/plan-id/image.jpg",
            storage.GetPublicUrl("plan-id/image.jpg"));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
