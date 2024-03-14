using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Plugins;
using System.Threading.Tasks;
using Terminal.Gui;
using Microsoft.SemanticKernel.TextToAudio;
using Options;
using System;


namespace SKSampleCatalog.Scenarios
{
    [ScenarioMetadata(Name: "Dynamic plugins", Description: "Shows how give the AI plugins to dynamically call.", OrderPriority: 1)]
    public class ChatWithDynamicPlugins : Scenario
    {
        private ChatView chatView;

        public override async Task Run()
        {
            // Get the Hue Bridge configuration
            HostApplicationBuilder builder = Host.CreateApplicationBuilder();
            builder.Services.Configure<HueBridgeOptions>(builder.Configuration.GetSection("HueBridgeOptions"));
            builder.Services.Configure<BingSearchOptions>(builder.Configuration.GetSection("BingSearchOptions"));
            var host = builder.Build();

            // Create a kernel builder
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddSingleton(host.Services.GetRequiredService<IOptions<HueBridgeOptions>>().Value);
            kernelBuilder.Services.AddSingleton(host.Services.GetRequiredService<IOptions<BingSearchOptions>>().Value);
            kernelBuilder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));

            // Option 1: Use a chat completion model from OpenAI
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: "gpt-4",
                apiKey: "[Your OpenAI API key]"
            );
            kernelBuilder.AddOpenAITextToAudio(
                modelId: "[Your chat completion model]",
                apiKey: "[Your OpenAI API key]"
            );

            // Option 2: Use a chat completion model from Azure OpenAI
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: "[The name of your deployment]",
                endpoint: "[Your Azure endpoint]",
                apiKey: "[Your Azure OpenAI API key]",
                modelId: "[The name of the model]" // optional
            );
            kernelBuilder.AddAzureOpenAITextToAudio(
                deploymentName: "[The name of your deployment]",
                endpoint: "[Your Azure endpoint]",
                apiKey: "[Your Azure OpenAI API key]",
                modelId: "[The name of the model]" // optional
            );

            // Add the plugins
            kernelBuilder.Plugins.AddFromType<HueLights>();
            kernelBuilder.Plugins.AddFromType<Bing>();
            kernelBuilder.Plugins.AddFromFunctions("DateTimeHelpers",
                [KernelFunctionFactory.CreateFromMethod(() => $"{DateTime.UtcNow:r}", "Now", "Gets the current date and time")]
            );

            // Build the kernel and retrieve the AI services
            Kernel kernel = kernelBuilder.Build();
            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            ITextToAudioService textToAudioService = kernel.GetRequiredService<ITextToAudioService>();

            ChatHistory history = new();
            history.AddSystemMessage("""
                You are a home assistant that can control lights.
                Before changing the lights, you may need to check their current state.
                Avoid telling the user numbers like the saturation, brightness, and hue; instead, use adjectives like 'bright' or 'dark'.
            """);

            while (true)
            {
                // Get the user's input
                Console.WriteLine("User > ");
                var input = Console.ReadLine();
                history.AddUserMessage(input);

                // Generate the bot's response using the chat completion service
                var response = chatCompletionService.GetStreamingChatMessageContentsAsync(
                    chatHistory: history,
                    executionSettings: new OpenAIPromptExecutionSettings()
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
                    },
                    kernel: kernel
                ).ConfigureAwait(false);

                // Stream the bot's response to the console
                Console.Write("Bot > ");
                string botResponse = "";
                await foreach (var message in response)
                {
                    botResponse += message.ToString();
                    Console.Write(message.ToString());
                }
                history.AddAssistantMessage(botResponse);

                // Convert the bot's response to audio
                var audio = await textToAudioService.GetAudioContentAsync(botResponse, new OpenAITextToAudioExecutionSettings("alloy")
                {
                    ResponseFormat = "wav",
                    Speed = 1.5f
                }).ConfigureAwait(false);
                AudioPlayer.PlaySound(audio.Data);
            }
        }

        public override void InitGui(ColorScheme colorScheme)
        {
            chatView = new ChatView(Console.CollectInput);
            Console.SetAddOutputChannel(chatView.AddResponse);
            Console.SetAppendOutputChannel(chatView.AppendResponse);
            Application.Top.Add(chatView);
        }
    }
}