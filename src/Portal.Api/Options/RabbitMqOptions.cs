using System.ComponentModel.DataAnnotations;

namespace Portal.Api.Service.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required] public string Host { get; set; } = "";
    [Required] public string VirtualHost { get; set; } = "/";
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}