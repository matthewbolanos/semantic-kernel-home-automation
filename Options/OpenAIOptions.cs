using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Options;

public sealed class OpenAIOptions : IValidatableObject
{
    public OpenAISource Source { get; set; } = OpenAISource.OpenAI;

    [Required]
    public string ChatModelId { get; set; } = string.Empty;

    [Required]
    public string TextToSpeechModelId { get; set; } = string.Empty;

    [Required]
    public string SpeechToTextModelId { get; set; } = string.Empty;

    [Required]
    public string ApiKey { get; set; } = string.Empty;

    public string ChatDeploymentName { get; set; } = string.Empty;

    public string Endpoint { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Source == OpenAISource.AzureOpenAI)
        {
            if (string.IsNullOrWhiteSpace(ChatDeploymentName))
            {
                yield return new ValidationResult(
                    "ChatDeploymentName is required when Source is AzureOpenAI.",
                    new[] { nameof(ChatDeploymentName) });
            }

            if (string.IsNullOrWhiteSpace(Endpoint))
            {
                yield return new ValidationResult(
                    "Endpoint is required when Source is AzureOpenAI.",
                    new[] { nameof(Endpoint) });
            }
        }
    }
}

public enum OpenAISource
{
    OpenAI,
    AzureOpenAI
}