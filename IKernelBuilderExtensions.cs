using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Options;

namespace SKSampleCatalog
{
    public static class IKernelBuilderExtensions
    {
        private static OpenAIOptions openAIOptions;

        private static OpenAIOptions GetOpenAIOptions()
        {
            if (openAIOptions != null)
            {
                return openAIOptions;
            }

            HostApplicationBuilder hostBuilder = Host.CreateApplicationBuilder();
            hostBuilder.Services.AddOptions<OpenAIOptions>()
                        .Bind(hostBuilder.Configuration.GetSection(nameof(OpenAIOptions)))
                        .ValidateDataAnnotations();
            var host = hostBuilder.Build();

            openAIOptions = host.Services.GetService<IOptions<OpenAIOptions>>()?.Value;
            return openAIOptions;
        }

        public static IKernelBuilder AddOpenAIChatCompletion(this IKernelBuilder builder, string modelId, string apiKey)
        {
            var openAIOptions = GetOpenAIOptions();

            if (openAIOptions.Source != OpenAISource.OpenAI)
            {
                return builder;
            }

            OpenAIServiceCollectionExtensions.AddOpenAIChatCompletion(
                builder,
                modelId: openAIOptions.ChatModelId,
                apiKey: openAIOptions.ApiKey
            );
            return builder;
        }
        public static IKernelBuilder AddOpenAITextToAudio(this IKernelBuilder builder, string modelId, string apiKey)
        {
            var openAIOptions = GetOpenAIOptions();

            if (openAIOptions.Source != OpenAISource.OpenAI)
            {
                return builder;
            }

            OpenAIServiceCollectionExtensions.AddOpenAITextToAudio(
                builder,
                modelId: openAIOptions.TextToSpeechModelId,
                apiKey: openAIOptions.ApiKey
            );
            return builder;
        }

        public static IKernelBuilder AddAzureOpenAIChatCompletion(this IKernelBuilder builder, string deploymentName, string endpoint, string apiKey, string modelId)
        {
            var openAIOptions = GetOpenAIOptions();

            if (openAIOptions.Source != OpenAISource.AzureOpenAI)
            {
                return builder;
            }

            OpenAIServiceCollectionExtensions.AddAzureOpenAIChatCompletion(
                builder,
                deploymentName: openAIOptions.ChatDeploymentName,
                endpoint: openAIOptions.Endpoint,
                apiKey: openAIOptions.ApiKey,
                modelId: openAIOptions.ChatModelId
            );

            return builder;
        }

        public static IKernelBuilder AddAzureOpenAITextToAudio(this IKernelBuilder builder, string deploymentName, string endpoint, string apiKey, string modelId)
        {
            var openAIOptions = GetOpenAIOptions();

            if (openAIOptions.Source != OpenAISource.AzureOpenAI)
            {
                return builder;
            }

            OpenAIServiceCollectionExtensions.AddAzureOpenAITextToAudio(
                builder,
                deploymentName: openAIOptions.ChatDeploymentName,
                endpoint: openAIOptions.Endpoint,
                apiKey: openAIOptions.ApiKey,
                modelId: openAIOptions.TextToSpeechModelId
            );

            return builder;
        }
    }
}