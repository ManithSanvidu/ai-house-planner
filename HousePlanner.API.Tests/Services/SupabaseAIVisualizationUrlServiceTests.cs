using System.Net;
using System.Text;
using HousePlanner.API.Services;
using Microsoft.Extensions.Configuration;

namespace HousePlanner.API.Tests.Services;

public class SupabaseAIVisualizationUrlServiceTests
{
    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SUPABASE_URL"] = "https://project.supabase.co",
            ["SUPABASE_SERVICE_ROLE_KEY"] = "server-secret",
            ["SUPABASE_AI_VISUALIZATION_BUCKET"] = "ai-visualizations",
        })
        .Build();

    [Theory]
    [InlineData(
        "/object/sign/ai-visualizations/file.png?token=x",
        "https://project.supabase.co/storage/v1/object/sign/ai-visualizations/file.png?token=x")]
    [InlineData(
        "/storage/v1/object/sign/ai-visualizations/file.png?token=x",
        "https://project.supabase.co/storage/v1/object/sign/ai-visualizations/file.png?token=x")]
    [InlineData(
        "https://storage.example.test/object/sign/ai-visualizations/file.png?token=x",
        "https://storage.example.test/object/sign/ai-visualizations/file.png?token=x")]
    public async Task ObjectKey_IsExchangedForNormalizedShortLivedSignedHttpsUrl(
        string signedUrl,
        string expectedUrl)
    {
        var handler = new SignedUrlHandler(signedUrl);
        var service = new SupabaseAIVisualizationUrlService(new HttpClient(handler), Configuration());

        var result = await service.GetReadUrlAsync("visualizations/workflow-1/image.png");

        Assert.Equal(expectedUrl, result);
        Assert.Equal(
            "https://project.supabase.co/storage/v1/object/sign/ai-visualizations/visualizations/workflow-1/image.png",
            handler.Request!.RequestUri!.ToString());
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("Bearer", handler.Request!.Headers.Authorization!.Scheme);
        Assert.Equal("server-secret", handler.Request.Headers.Authorization.Parameter);
        Assert.Contains("\"expiresIn\":1800", handler.Body);
        Assert.DoesNotContain("server-secret", result);
    }

    [Theory]
    [InlineData("http://localhost:8001/visualizations/legacy.png")]
    [InlineData("https://cdn.example.test/legacy.png")]
    public async Task LegacyAbsoluteUrl_IsReturnedWithoutSigning(string legacyUrl)
    {
        var handler = new SignedUrlHandler();
        var service = new SupabaseAIVisualizationUrlService(new HttpClient(handler), Configuration());

        Assert.Equal(legacyUrl, await service.GetReadUrlAsync(legacyUrl));
        Assert.Null(handler.Request);
    }

    private sealed class SignedUrlHandler : HttpMessageHandler
    {
        private readonly string _signedUrl;

        public SignedUrlHandler(string signedUrl = "/storage/v1/object/sign/ai-visualizations/visualizations/workflow-1/image.png?token=test-token")
        {
            _signedUrl = signedUrl;
        }

        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"signedURL\":\"{_signedUrl}\"}}",
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
