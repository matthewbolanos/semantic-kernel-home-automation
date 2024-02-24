using System.ComponentModel.DataAnnotations;

namespace Options;


public sealed class HueBridgeOptions
{
    [Required]
    public string IpAddress { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
}