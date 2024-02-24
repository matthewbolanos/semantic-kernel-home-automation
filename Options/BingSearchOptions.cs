using System.ComponentModel.DataAnnotations;

namespace Options;


public sealed class BingSearchOptions
{
    [Required]
    public string ApiKey { get; set; } = string.Empty;
    [Required]
    public string Endpoint { get; set; } = string.Empty;
}