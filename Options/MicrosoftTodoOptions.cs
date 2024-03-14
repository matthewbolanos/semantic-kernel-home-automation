using System.ComponentModel.DataAnnotations;

namespace Options;


public sealed class MicrosoftTodoOptions
{
    [Required]
    public string TenantId { get; set; } = string.Empty;
    [Required]
    public string ClientId { get; set; } = string.Empty;
    [Required]
    public string ClientSecret { get; set; } = string.Empty;
    [Required]
    public string TaskListId { get; set; } = string.Empty;
    [Required]
    public string NotificationUrl { get; set; } = string.Empty;
}