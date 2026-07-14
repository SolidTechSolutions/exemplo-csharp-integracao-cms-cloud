using System.IO.Compression;
using System.Text.Json;

namespace SolidSign.Examples;

/// <summary>
/// [EN]    CAdES (CMS) signing — Cloud HSM / PSC (one-step).
///         Cloud credentials (credentialId + SAD) + signaturePackaging for CMS format.
/// [PT-BR] Assinatura CAdES (CMS) — Cloud HSM / PSC (passo único).
///         Credenciais de nuvem (credentialId + SAD) + signaturePackaging para formato CMS.
/// </summary>
public class CmsCloudService(IConfiguration cfg, HttpClient http)
{
    private string BaseUrl      => cfg["SolidSign:Api:BaseUrl"]!.TrimEnd('/');
    private string Auth         => cfg["SolidSign:Api:Authorization"]!;
    private string CredentialId => cfg["SolidSign:Cloud:CredentialId"]!;
    private string SAD          => cfg["SolidSign:Cloud:SAD"]!;
    private string Profile      => cfg["SolidSign:Sig:Profile"] ?? "ADRB";
    private string HashAlg      => cfg["SolidSign:Sig:HashAlgorithm"] ?? "SHA256";
    private string PolicyVer    => cfg["SolidSign:Sig:PolicyVersion"] ?? "";
    private string Packaging    => cfg["SolidSign:Sig:SignaturePackaging"] ?? "ENVELOPING";

    // ── Batch endpoint ────────────────────────────────────────────────────────

    public async Task<string?> SignHsmCloudAsync(IEnumerable<string> filePaths, string outputDir)
    {
        using var form = BuildForm(filePaths);
        form.Add(new StringContent(CredentialId), "credentialId");
        form.Add(new StringContent(SAD),          "sad");
        form.Add(new StringContent(Profile),      "profile");
        form.Add(new StringContent(HashAlg),      "hashAlgorithm");
        form.Add(new StringContent(Packaging),    "signaturePackaging");
        if (!string.IsNullOrWhiteSpace(PolicyVer)) form.Add(new StringContent(PolicyVer), "policyVersion");

        var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/solidsign/dsig/cms/sign-hsm-cloud") { Content = form };
        req.Headers.TryAddWithoutValidation("Authorization", Auth);
        var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) { Console.Error.WriteLine($"SolidSign error {(int)resp.StatusCode}: {await resp.Content.ReadAsStringAsync()}"); return null; }

        var signResp = JsonSerializer.Deserialize<SignResponse>(await resp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var zipBytes = await DownloadAndZipAsync(signResp!, filePaths.Select(Path.GetFileName!).ToList(), Auth);

        Directory.CreateDirectory(outputDir);
        var outPath = Path.Combine(outputDir, $"signed_cms_cloud_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.zip");
        await File.WriteAllBytesAsync(outPath, zipBytes);
        Console.WriteLine($"CAdES Cloud signing complete. Output: {outPath}");
        return outPath;
    }

    // ── Form endpoint ─────────────────────────────────────────────────────────

    public async Task<byte[]?> SignHsmCloudFormAsync(
        string authorization, string baseUrl,
        string credentialId, string sad,
        IEnumerable<IFormFile> documents,
        string? profile, string? hashAlgorithm, string? policyVersion,
        string? signaturePackaging,
        string? encapsulatedTimestampConfig = null,
        string? documentInfoMetadata = null)
    {
        using var form = new MultipartFormDataContent();
        int i = 0;
        foreach (var d in documents) { var ms = new MemoryStream(); await d.CopyToAsync(ms); ms.Position = 0; form.Add(new StreamContent(ms), $"document[{i++}]", d.FileName); }
        form.Add(new StringContent(credentialId), "credentialId");
        form.Add(new StringContent(sad), "sad");
        void Add(string? v, string k) { if (!string.IsNullOrWhiteSpace(v)) form.Add(new StringContent(v), k); }
        Add(profile, "profile"); Add(hashAlgorithm, "hashAlgorithm"); Add(policyVersion, "policyVersion");
        Add(signaturePackaging, "signaturePackaging");
        Add(encapsulatedTimestampConfig, "encapsulatedTimestampConfig");
        Add(documentInfoMetadata, "documentInfoMetadata");

        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/solidsign/dsig/cms/sign-hsm-cloud") { Content = form };
        req.Headers.TryAddWithoutValidation("Authorization", authorization);
        var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) { Console.Error.WriteLine($"SolidSign error {(int)resp.StatusCode}"); return null; }

        var signResp = JsonSerializer.Deserialize<SignResponse>(await resp.Content.ReadAsStringAsync(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return await DownloadAndZipAsync(signResp!, documents.Select(d => (string?)d.FileName).ToList(), authorization);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static MultipartFormDataContent BuildForm(IEnumerable<string> filePaths)
    {
        var form = new MultipartFormDataContent();
        int i = 0;
        foreach (var fp in filePaths) form.Add(new StreamContent(File.OpenRead(fp)), $"document[{i++}]", Path.GetFileName(fp));
        return form;
    }

    private async Task<byte[]> DownloadAndZipAsync(SignResponse signResp, List<string?> originalNames, string auth)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (int i = 0; i < signResp.Documents.Count; i++)
            {
                var href = signResp.Documents[i].HalLinks?.Self?.Href
                        ?? signResp.Documents[i].Links?.FirstOrDefault(l => l.Rel == "self")?.Href;
                if (href is null) continue;
                var dlReq = new HttpRequestMessage(HttpMethod.Get, href);
                dlReq.Headers.TryAddWithoutValidation("Authorization", auth);
                var dlResp = await http.SendAsync(dlReq);
                if (!dlResp.IsSuccessStatusCode) continue;
                var entry = archive.CreateEntry($"signed_{originalNames[i]}");
                await using var entryStream = entry.Open();
                await (await dlResp.Content.ReadAsStreamAsync()).CopyToAsync(entryStream);
            }
        }
        return ms.ToArray();
    }
}
