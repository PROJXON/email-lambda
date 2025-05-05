using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

// Lambda interface
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EmailLambda
{
    public class EmailRequest
    {
        public string To { get; set; } = "";
        public string Name { get; set; } = "";
        public string Course { get; set; } = "";
        public string Base64Pdf { get; set; } = "";
        public string FileName { get; set; } = "";
        public int CertificateType { get; set; }
    }

    public class Function
    {
        public static async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                var emailData = JsonSerializer.Deserialize<EmailRequest>(request.Body);

                var smtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL")!;
                var smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")!;
                var smtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER")!;
                var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT")!);

                var mailMessage = new MailMessage();

                if (emailData == null || string.IsNullOrWhiteSpace(emailData.To))
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Invalid input"
                    };
                }

                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpEmail, smtpPassword),
                    EnableSsl = true
                };

                mailMessage = emailData.CertificateType switch
                {
                    1 => CreateCanvasEmail(emailData, smtpEmail),
                    2 => CreateGraduationEmail(emailData, smtpEmail),
                    _ => mailMessage
                };

                using (mailMessage)
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Headers = new Dictionary<string, string>
                    {
                    { "Content-Type", "application/json" }
                    },
                    Body = $"{{ \"message\": \"Email sent to {emailData.To}\" }}"
                };
            }
            catch (Exception ex)
            {

                context.Logger.LogError($"Exception: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = $"Internal Server Error: {ex.Message}"
                };
            }
        }

        private static MailMessage CreateCanvasEmail(EmailRequest emailData, string smtpEmail)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpEmail, "PROJXON Programs"),
                Subject = $"{emailData.Course} Certificate",
                Body = $"Congratulations, {emailData.Name}!\n\n This is your certificate for the successful completion of the {emailData.Course} course on Canvas.",
                IsBodyHtml = false
            };

            mailMessage.To.Add(emailData.To);

            var attachment = AddAttachment(emailData);

            mailMessage.Attachments.Add(attachment);

            return mailMessage;
        }

        private static MailMessage CreateGraduationEmail(EmailRequest emailData, string smtpEmail)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpEmail, "PROJXON Programs"),
                Subject = "MIP Graduation Certificate",
                Body = $"Congratulations, {emailData.Name}!\n\n This is your certificate for the successful completion of the Momentum Internship Program with PROJXON.",
                IsBodyHtml = false
            };

            mailMessage.To.Add(emailData.To);

            var attachment = AddAttachment(emailData);

            mailMessage.Attachments.Add(attachment);

            return mailMessage;
        }

        private static Attachment AddAttachment(EmailRequest emailData)
        {
            var pdfBytes = Convert.FromBase64String(emailData.Base64Pdf);
            return new Attachment(new MemoryStream(pdfBytes), emailData.FileName, "application/pdf");
        }
    }
}
