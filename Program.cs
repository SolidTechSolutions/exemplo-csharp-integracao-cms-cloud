/**
 * [EN]    CAdES (CMS) signing example — Cloud HSM / PSC (one-step).
 *         Run: dotnet run
 *         Batch: POST http://localhost:5093/api/cms/sign-cloud
 *         Form:  POST http://localhost:5093/api/cms/sign/form
 *
 * [PT-BR] Exemplo de assinatura CAdES (CMS) — Cloud HSM / PSC (passo único).
 *         Executar: dotnet run
 */
using SolidSign.Examples;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<CmsCloudService>();
var app = builder.Build();

// ── Batch endpoint ──────────────────────────────────────────────────────────
app.MapPost("/api/cms/sign-cloud", async (CmsCloudService svc, IConfiguration cfg) =>
{
    var inputPath  = cfg["SolidSign:Batch:InputPath"] ?? "";
    var outputPath = cfg["SolidSign:Batch:OutputPath"] ?? "";

    if (!Directory.Exists(inputPath)) return Results.BadRequest(new { error = $"Invalid input path: {inputPath}" });
    var files = Directory.GetFiles(inputPath);
    if (files.Length == 0) return Results.Ok(new { message = $"No files found in {inputPath}" });

    Console.WriteLine($"Found {files.Length} files for cloud processing.");
    var result = await svc.SignHsmCloudAsync(files, outputPath);

    return result is not null
        ? Results.Ok(new { message = $"Processing completed! ZIP generated at: {result}" })
        : Results.Problem("Processing failed. Check logs.");
});

// ── Form endpoint ───────────────────────────────────────────────────────────
app.MapPost("/api/cms/sign/form", async (HttpRequest req, CmsCloudService svc) =>
{
    var form = await req.ReadFormAsync();
    var documents = form.Files.GetFiles("document");
    string? G(string k) => form.TryGetValue(k, out var v) ? v.ToString() : null;

    var zip = await svc.SignHsmCloudFormAsync(
        authorization: G("authorization")!, baseUrl: G("baseUrl")!,
        credentialId: G("credentialId")!, sad: G("sad")!,
        documents: documents,
        profile: G("profile"), hashAlgorithm: G("hashAlgorithm"), policyVersion: G("policyVersion"),
        signaturePackaging: G("signaturePackaging"),
        encapsulatedTimestampConfig: G("encapsulatedTimestampConfig"),
        documentInfoMetadata: G("documentInfoMetadata"));

    return zip is not null
        ? Results.File(zip, "application/zip", "signed_cms.zip")
        : Results.Problem("Processing failed. Check logs.");
});

app.Run("http://0.0.0.0:5093");
