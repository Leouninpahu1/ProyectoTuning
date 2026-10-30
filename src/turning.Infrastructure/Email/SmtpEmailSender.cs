using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Turning.Application.Interfaces;

namespace Turning.Infrastructure.Email;

/// <summary>
/// Envía correo por SMTP con STARTTLS, o lo deja como .eml en <see cref="EmailOptions.PickupDirectory"/>.
/// </summary>
/// <remarks>
/// shortcut: SmtpClient no soporta OAuth2 ni TLS implícito (puerto 465); si Google retira las
/// contraseñas de aplicación o el proveedor exige OAuth2, cambiar este adaptador a MailKit.
/// </remarks>
public sealed class SmtpEmailSender : IEmailSender
{
    private static readonly TimeSpan SendTimeout = TimeSpan.FromSeconds(10);
    private readonly EmailOptions _options;

    /// <summary>
    /// Constructor del adaptador SMTP.
    /// </summary>
    public SmtpEmailSender(IOptions<EmailOptions> options) => _options = options.Value;

    /// <inheritdoc />
    public async Task SendAsync(string to, string subject, string textBody, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage(
            _options.From ?? throw new InvalidOperationException("Falta Email:From."), to, subject, textBody);
        using var client = new SmtpClient();

        if (!string.IsNullOrWhiteSpace(_options.PickupDirectory))
        {
            var folder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(_options.PickupDirectory));
            Directory.CreateDirectory(folder);
            client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
            client.PickupDirectoryLocation = folder;
        }
        else
        {
            client.Host = _options.Smtp.Host ?? throw new InvalidOperationException("Falta Email:Smtp:Host.");
            client.Port = _options.Smtp.Port;
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(_options.Smtp.Username, _options.Smtp.Password);
        }

        // SmtpClient.Timeout solo aplica al envío síncrono: el límite del envío asíncrono va por token.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(SendTimeout);
        await client.SendMailAsync(message, timeout.Token);
    }
}
