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
    private static byte[]? _cachedDerBytes;
    private static byte[]? _cachedPemBytes;
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
        ViewBag.AppUrl = $"{baseUrl}/garcom?source=pwa";
        ViewBag.ManifestUrl = $"{baseUrl}/manifest.json";

        return View();
    }

    /// <summary>
    /// Descarrega o pacote instalador Android APK nativo (.apk)
    /// </summary>
    [HttpGet("download/android")]
    [HttpHead("download/android")]
    [HttpGet("download/apk")]
    [HttpHead("download/apk")]
    public IActionResult DownloadAndroid()
    {
        var apkPath = Path.Combine(_env.WebRootPath, "downloads", "MenovaGarcom.apk");
        if (System.IO.File.Exists(apkPath))
        {
            var bytes = System.IO.File.ReadAllBytes(apkPath);
            Response.Headers.ContentLength = bytes.Length;
            return File(bytes, "application/vnd.android.package-archive", "MenovaGarcom.apk", enableRangeProcessing: true);
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Descarrega o Perfil WebClip Apple (.mobileconfig) para iPhone / iPad
    /// </summary>
    [HttpGet("download/ios")]
    [HttpHead("download/ios")]
    public IActionResult DownloadIos()
    {
        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        var appUrl = $"{scheme}://{host}/garcom?source=pwa";

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
        Response.Headers["Content-Disposition"] = "inline; filename=\"MenovaGarcom.mobileconfig\"";

        return File(bytes, "application/x-apple-aspen-config", "MenovaGarcom.mobileconfig", enableRangeProcessing: true);
    }

    /// <summary>
    /// Descarrega o Certificado Raiz em formato binário DER (.crt) - Padrão nativo para Android e Windows
    /// </summary>
    [HttpGet("download/crt")]
    [HttpHead("download/crt")]
    [HttpGet("/instalar-certificado/download")]
    [HttpHead("/instalar-certificado/download")]
    [HttpGet("/instalar-certificado/download/crt")]
    [HttpHead("/instalar-certificado/download/crt")]
    public IActionResult DownloadCrt()
    {
        var (derBytes, _) = ObterOuGerarCertificadoRaiz();
        Response.Headers.ContentLength = derBytes.Length;

        return File(derBytes, "application/x-x509-ca-cert", "menova-root.crt", enableRangeProcessing: true);
    }

    /// <summary>
    /// Descarrega o Certificado Raiz em formato PEM (.pem / .cer)
    /// </summary>
    [HttpGet("download/pem")]
    [HttpHead("download/pem")]
    [HttpGet("/instalar-certificado/download/pem")]
    [HttpHead("/instalar-certificado/download/pem")]
    public IActionResult DownloadPem()
    {
        var (_, pemBytes) = ObterOuGerarCertificadoRaiz();
        Response.Headers.ContentLength = pemBytes.Length;

        return File(pemBytes, "application/x-pem-file", "menova-root.pem", enableRangeProcessing: true);
    }

    /// <summary>
    /// Instala o Certificado Raiz no iPhone / iPad como Perfil de Configuração Apple (.mobileconfig)
    /// </summary>
    [HttpGet("download/cert-ios")]
    [HttpHead("download/cert-ios")]
    [HttpGet("/instalar-certificado/download/ios")]
    [HttpHead("/instalar-certificado/download/ios")]
    public IActionResult DownloadCertIos()
    {
        var (derBytes, _) = ObterOuGerarCertificadoRaiz();
        var base64Cert = Convert.ToBase64String(derBytes);

        var certProfileXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>ConsentText</key>
    <dict>
        <key>default</key>
        <string>Instalação do Certificado de Autoridade de Segurança Raiz Menova Restaurante para tráfego HTTPS local.</string>
    </dict>
    <key>PayloadContent</key>
    <array>
        <dict>
            <key>PayloadCertificateFileName</key>
            <string>menova-root.cer</string>
            <key>PayloadContent</key>
            <data>
{base64Cert}
            </data>
            <key>PayloadDescription</key>
            <string>Adiciona o certificado CA Raiz Menova aos certificados confiáveis.</string>
            <key>PayloadDisplayName</key>
            <string>Menova CA Raiz Local</string>
            <key>PayloadIdentifier</key>
            <string>mz.menova.cert.root</string>
            <key>PayloadType</key>
            <string>com.apple.security.root</string>
            <key>PayloadUUID</key>
            <string>B6D21838-8B77-4C3D-8A7E-97FF531B52F2</string>
            <key>PayloadVersion</key>
            <integer>1</integer>
        </dict>
    </array>
    <key>PayloadDescription</key>
    <string>Certificado de Autoridade Raiz Local Menova Restaurante</string>
    <key>PayloadDisplayName</key>
    <string>Menova CA Raiz Local</string>
    <key>PayloadIdentifier</key>
    <string>mz.menova.profile.cert</string>
    <key>PayloadOrganization</key>
    <string>Menova Restaurante</string>
    <key>PayloadRemovalDisallowed</key>
    <false/>
    <key>PayloadType</key>
    <string>Configuration</string>
    <key>PayloadUUID</key>
    <string>A51B7C69-0A67-4F47-99F3-3D62F6FAEFF9</string>
    <key>PayloadVersion</key>
    <integer>1</integer>
</dict>
</plist>";

        var bytes = Encoding.UTF8.GetBytes(certProfileXml);
        Response.Headers.ContentLength = bytes.Length;
        Response.Headers["Content-Disposition"] = "inline; filename=\"MenovaCertificadoRaiz.mobileconfig\"";

        return File(bytes, "application/x-apple-aspen-config", "MenovaCertificadoRaiz.mobileconfig", enableRangeProcessing: true);
    }

    private (byte[] Der, byte[] Pem) ObterOuGerarCertificadoRaiz()
    {
        if (_cachedDerBytes != null && _cachedPemBytes != null)
        {
            return (_cachedDerBytes, _cachedPemBytes);
        }

        lock (_crtLock)
        {
            if (_cachedDerBytes != null && _cachedPemBytes != null)
            {
                return (_cachedDerBytes, _cachedPemBytes);
            }

            var certDir = Path.Combine(_env.ContentRootPath, "certs");
            Directory.CreateDirectory(certDir);
            var derFile = Path.Combine(certDir, "menova-root.der");
            var pemFile = Path.Combine(certDir, "menova-root.pem");

            if (System.IO.File.Exists(derFile) && System.IO.File.Exists(pemFile))
            {
                _cachedDerBytes = System.IO.File.ReadAllBytes(derFile);
                _cachedPemBytes = System.IO.File.ReadAllBytes(pemFile);
                return (_cachedDerBytes, _cachedPemBytes);
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

            _cachedDerBytes = cert.Export(X509ContentType.Cert);
            _cachedPemBytes = Encoding.UTF8.GetBytes(cert.ExportCertificatePem());

            System.IO.File.WriteAllBytes(derFile, _cachedDerBytes);
            System.IO.File.WriteAllBytes(pemFile, _cachedPemBytes);

            return (_cachedDerBytes, _cachedPemBytes);
        }
    }
}
