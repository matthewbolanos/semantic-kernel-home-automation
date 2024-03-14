using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.SemanticKernel;
using Options;

#nullable enable

namespace Plugins;

public class Todo
{
    private GraphServiceClient client;
    private MicrosoftTodoOptions options;

    public bool IsOn { get; set; } = false;

    public Todo(GraphServiceClient graphServiceClient, MicrosoftTodoOptions microsoftTodoOptions)
    {
        client = graphServiceClient;
        options = microsoftTodoOptions;
    }

    [KernelFunction("MarkTaskComplete")]
    public async Task<string> MarkTaskComplete(string id)
    {
        var taskToComplete = await client.Me.Todo.Lists[options.TaskListId].Tasks[id].GetAsync().ConfigureAwait(false);

        if (taskToComplete == null)
        {
            return "Task not found";
        }

        taskToComplete.Status = Microsoft.Graph.Models.TaskStatus.Completed;

        await client.Me.Todo.Lists[options.TaskListId].Tasks[id].PatchAsync(taskToComplete).ConfigureAwait(false);

        return "Task marked completed";
    }
}