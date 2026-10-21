using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Menova.Web.Controllers;

[AllowAnonymous]
[Route("app")]
public class AppController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AppController> _logger;
    private static byte[]? _cachedCrtBytes;
    private static readonly object _crtLock = new();

    public AppController(IWebHostEnvironment env, ILogger<AppController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpGet("")]
    [HttpHead("")]
    [HttpGet("/garcom/app")]
    [HttpHead("/garcom/app")]
    public IActionResult Index()
    {
        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        var baseUrl = $"{scheme}://{host}";

        ViewBag.BaseUrl = baseUrl;
        ViewBag.AppUrl = $"{baseUrl}/garcom";
        ViewBag.ManifestUrl = $"{baseUrl}/manifest.json";

        return View();
    }

    [HttpGet("download/android")]
    [HttpHead("download/android")]
    public IActionResult DownloadAndroid()
    {
        var apkPath = Path.Combine(_env.WebRootPath, "downloads", "MenovaGarcom.apk");
        if (!System.IO.File.Exists(apkPath))
        {
            _logger.LogWarning("Ficheiro MenovaGarcom.apk não encontrado em {Path}. A gerar ficheiro dinâmico...", apkPath);
            return NotFound("Ficheiro de instalação Android temporariamente indisponível.");
        }

        var bytes = System.IO.File.ReadAllBytes(apkPath);
        Response.Headers.ContentLength = bytes.Length;

        return File(bytes, "application/vnd.android.package-archive", "MenovaGarcom.apk", enableRangeProcessing: true);
    }

    [HttpGet("download/ios")]
    [HttpHead("download/ios")]
    public IActionResult DownloadIos()
    {
        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        var appUrl = $"{scheme}://{host}/garcom";

        // Obter ícone em Base64
        var iconPath = Path.Combine(_env.WebRootPath, "icons", "icon-180.png");
        string base64Icon = string.Empty;
        if (System.IO.File.Exists(iconPath))
        {
            base64Icon = Convert.ToBase64String(System.IO.File.ReadAllBytes(iconPath));
        }

        var mobileConfigXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>ConsentText</key>
    <dict>
        <key>default</key>
        <string>Instalação do atalho dedicado Menova Garçom para o restaurante local.</string>
    </dict>
    <key>PayloadContent</key>
    <array>
        <dict>
            <key>FullScreen</key>
            <true/>
            <key>Icon</key>
            <data>
{base64Icon}
            </data>
            <key>IsRemovable</key>
            <true/>
            <key>Label</key>
            <string>Menova Garçom</string>
            <key>PayloadDescription</key>
            <string>Configuração do WebClip Menova Garçom</string>
            <key>PayloadDisplayName</key>
            <string>Menova Garçom</string>
            <key>PayloadIdentifier</key>
            <string>mz.menova.garcom.webclip</string>
            <key>PayloadType</key>
            <string>com.apple.webClip.managed</string>
            <key>PayloadUUID</key>
            <string>D9585E22-5B26-4CB2-B734-7D6797D74378</string>
            <key>PayloadVersion</key>
            <integer>1</integer>
            <key>Precomposed</key>
            <true/>
            <key>URL</key>
            <string>{appUrl}</string>
        </dict>
    </array>
    <key>PayloadDescription</key>
    <string>Instala a aplicação Menova Garçom no ecrã principal do seu iPhone ou iPad.</string>
    <key>PayloadDisplayName</key>
    <string>Menova Garçom</string>
    <key>PayloadIdentifier</key>
    <string>mz.menova.garcom.profile</string>
    <key>PayloadOrganization</key>
    <string>Menova Restaurante</string>
    <key>PayloadRemovalDisallowed</key>
    <false/>
    <key>PayloadType</key>
    <string>Configuration</string>
    <key>PayloadUUID</key>
    <string>56C3BCE6-1528-44A5-A595-D98FA07EE668</string>
    <key>PayloadVersion</key>
    <integer>1</integer>
</dict>
</plist>";

        var bytes = Encoding.UTF8.GetBytes(mobileConfigXml);
        Response.Headers.ContentLength = bytes.Length;

        return File(bytes, "application/x-apple-aspen-config", "MenovaGarcom.mobileconfig", enableRangeProcessing: true);
    }

    [HttpGet("download/crt")]
    [HttpHead("download/crt")]
    [HttpGet("/instalar-certificado/download")]
    [HttpHead("/instalar-certificado/download")]
    [HttpGet("/instalar-certificado/download/crt")]
    [HttpHead("/instalar-certificado/download/crt")]
    public IActionResult DownloadCrt()
    {
        var bytes = ObterOuGerarCertificadoRaiz();
        Response.Headers.ContentLength = bytes.Length;

        return File(bytes, "application/x-x509-ca-cert", "menova-root.crt", enableRangeProcessing: true);
    }

    private byte[] ObterOuGerarCertificadoRaiz()
    {
        if (_cachedCrtBytes != null) return _cachedCrtBytes;

        lock (_crtLock)
        {
            if (_cachedCrtBytes != null) return _cachedCrtBytes;

            // Verificar se já existe um certificado gerado pelo Caddy ou sistema
            var certDir = Path.Combine(_env.ContentRootPath, "certs");
            Directory.CreateDirectory(certDir);
            var crtFile = Path.Combine(certDir, "menova-root.crt");

            if (System.IO.File.Exists(crtFile))
            {
                _cachedCrtBytes = System.IO.File.ReadAllBytes(crtFile);
                return _cachedCrtBytes;
            }

            // Gerar novo certificado raiz X.509 autofirmado para rede local
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=Menova CA Raiz Local, O=Menova Restaurante, OU=Sistemas POS, C=MZ",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.DigitalSignature,
                    critical: true));

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

            using var cert = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow.AddYears(10));

            // Exportar em formato PEM/DER
            var pem = cert.ExportCertificatePem();
            _cachedCrtBytes = Encoding.UTF8.GetBytes(pem);

            System.IO.File.WriteAllBytes(crtFile, _cachedCrtBytes);
            return _cachedCrtBytes;
        }
    }
}
