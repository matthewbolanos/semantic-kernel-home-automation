using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Plugins;
using Terminal.Gui;
using Options;
using System;
using Azure.Identity;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using Application = Terminal.Gui.Application;
using System.Collections.Generic;


namespace SKSampleCatalog.Scenarios
{
    [ScenarioMetadata(Name: "Home Automation By Task", Description: "Shows how the AI can respond to events.", OrderPriority: 2)]
    public class HomeAutomationByTask : Scenario
    {
        private ChatView chatView;

        public override async Task Run()
        {
            // create store for notification IDs
            var notificationIds = new HashSet<string>();
            var activeTasks = new List<string>();

            // Get the app settings configuration
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
            builder.Services.Configure<HueBridgeOptions>(builder.Configuration.GetSection("HueBridgeOptions"));
            builder.Services.Configure<BingSearchOptions>(builder.Configuration.GetSection("BingSearchOptions"));
            builder.Services.Configure<MicrosoftTodoOptions>(builder.Configuration.GetSection("MicrosoftTodoOptions"));
            var app = builder.Build();

            // Create Graph client
            var scopes = new[] { "User.Read", "Tasks.ReadWrite" };
            var microsoftGraphOptions = app.Services.GetRequiredService<IOptions<MicrosoftTodoOptions>>().Value;
            var interactiveOptions = new InteractiveBrowserCredentialOptions
            {
                TenantId = microsoftGraphOptions.TenantId,
                ClientId = microsoftGraphOptions.ClientId,
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                RedirectUri = new Uri("http://localhost"), // Specify the redirect URI; it should match the one set in the Azure portal
            };
            var interactiveCredential = new InteractiveBrowserCredential(interactiveOptions);
            var graphClient = new GraphServiceClient(interactiveCredential, scopes);

            // Create a kernel builder
            IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));
            kernelBuilder.Services.AddSingleton(app.Services.GetRequiredService<IOptions<HueBridgeOptions>>().Value);
            kernelBuilder.Services.AddSingleton(app.Services.GetRequiredService<IOptions<BingSearchOptions>>().Value);
            kernelBuilder.Services.AddSingleton(app.Services.GetRequiredService<IOptions<MicrosoftTodoOptions>>().Value);
            kernelBuilder.Services.AddSingleton(graphClient);

            // Option 1: Use a chat completion model from OpenAI
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: "gpt-4",
                apiKey: "[Your OpenAI API key]"
            );

            // Option 2: Use a chat completion model from Azure OpenAI
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: "[The name of your deployment]",
                endpoint: "[Your Azure endpoint]",
                apiKey: "[Your Azure OpenAI API key]",
                modelId: "[The name of the model]" // optional
            );

            // Add the plugins
            kernelBuilder.Plugins.AddFromFunctions("DateTimeHelpers",
                [KernelFunctionFactory.CreateFromMethod(() => $"{DateTime.UtcNow:r}", "Now", "Gets the current date and time")]
            );
            kernelBuilder.Plugins.AddFromType<Bing>();
            kernelBuilder.Plugins.AddFromType<HueLights>();
            kernelBuilder.Plugins.AddFromType<Plugins.Todo>();

            // Build the kernel and retrieve the AI services
            Kernel kernel = kernelBuilder.Build();
            IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Create chat history
            ChatHistory history = new();
            history.AddSystemMessage("""
                You are a home assistant that can control lights.
                Before changing the lights, you may need to check their current state.
                Avoid telling the user numbers like the saturation, brightness, and hue; instead, use adjectives like 'bright' or 'dark'.
            """);

            // Create route for the notification endpoint
            app.UseRouting();
            app.MapPost("/notification", async (HttpRequest request, HttpResponse response) =>
            {
                // Validate a new subscription by responding to the validation token
                if (request.Query.ContainsKey("validationToken"))
                {
                    var validationToken = request.Query["validationToken"].ToString();
                    response.ContentType = "text/plain";
                    await response.WriteAsync(validationToken).ConfigureAwait(false);
                    return;
                }

                // Check if notification has already been processed
                if (notificationIds.Contains(request.Headers["Request-Id"]))
                {
                    response.StatusCode = StatusCodes.Status200OK;
                    return;
                }
                notificationIds.Add(request.Headers["Request-Id"]);

                // Get the list of new open tasks
                int numberOfNewTasks = 0;
                var newTasks = new List<string>();
                var tasks = await graphClient.Me.Todo.Lists[microsoftGraphOptions.TaskListId].Tasks.GetAsync().ConfigureAwait(false);
                foreach (var task in tasks.Value)
                {
                    if (task.Status == Microsoft.Graph.Models.TaskStatus.Completed || activeTasks.Contains(task.Id))
                    {
                        continue;
                    }
                    activeTasks.Add(task.Id);
                    newTasks.Add(task.Id);
                    numberOfNewTasks++;

                    Console.WriteLine("You > " + task.Title);
                    history.AddUserMessage($"""
                        taskId: "{task.Id}"
                        taskTitle: "{task.Title}"
                    """);
                    history.AddSystemMessage("Once you are finished with a task, close it");
                }

                if (numberOfNewTasks > 0)
                {
                    // Generate the bot's response using the chat completion service
                    var assistantResponse = chatCompletionService.GetStreamingChatMessageContentsAsync(
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
                    await foreach (var message in assistantResponse)
                    {
                        botResponse += message.ToString();
                        Console.Write(message.ToString());
                    }
                    history.AddAssistantMessage(botResponse);

                    // remove the new tasks from the active tasks list
                    foreach (var task in newTasks)
                    {
                        activeTasks.Remove(task);
                    }
                }

                // Acknowledge receipt of the notification
                response.StatusCode = StatusCodes.Status202Accepted;
            });
            _ = Task.Run(() => app.Run("http://localhost:8000"));

            // Delete previous subscriptions
            var subscriptions = await graphClient.Subscriptions.GetAsync().ConfigureAwait(false);
            foreach (var subscription in subscriptions.Value)
            {
                await graphClient.Subscriptions[subscription.Id].DeleteAsync().ConfigureAwait(false);
            }

            // Create a new subscription
            var request = new Subscription
            {
                ChangeType = "created,updated",
                NotificationUrl = microsoftGraphOptions.NotificationUrl + "/notification",
                Resource = $"/me/todo/lists/{microsoftGraphOptions.TaskListId}/tasks",
                ExpirationDateTime = DateTimeOffset.UtcNow.AddMinutes(30),
                ClientState = "secretClientValue",
                LatestSupportedTlsVersion = "v1_2"
            };

            await graphClient.Subscriptions.PostAsync(request).ConfigureAwait(false);
        }

        public override void InitGui(ColorScheme colorScheme)
        {
            chatView = new ChatView(Console.CollectInput, showInput: false);
            Console.SetAddOutputChannel(chatView.AddResponse);
            Console.SetAppendOutputChannel(chatView.AppendResponse);
            Application.Top.Add(chatView);
        }
    }
}