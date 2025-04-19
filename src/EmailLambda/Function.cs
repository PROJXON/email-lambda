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
    }

    public class Function
    {
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            var emailData = JsonSerializer.Deserialize<EmailRequest>(request.Body);

            // Validate input
            if (emailData == null || string.IsNullOrWhiteSpace(emailData.To))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = 400,
                    Body = "Invalid input"
                };
            }

            string smtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL")!;
            string smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD")!;

            using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(smtpEmail, smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpEmail, "PROJXON Programs"),
                Subject = $"{emailData.Course} Certificate",
                Body = $"Congratulations, {emailData.Name}!\n\n This is your certificate for the successful completion of the {emailData.Course} course on Canvas.",
                IsBodyHtml = false
            };

            mailMessage.To.Add(emailData.To);

            var pdfBytes = Convert.FromBase64String(emailData.Base64Pdf);
            var attachment = new Attachment(new MemoryStream(pdfBytes), emailData.FileName, "application/pdf");
            mailMessage.Attachments.Add(attachment);

            await smtpClient.SendMailAsync(mailMessage);

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
    }
}
