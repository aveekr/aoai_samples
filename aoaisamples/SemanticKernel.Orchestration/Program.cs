using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.Plugins.Models;
using SemanticKernel.Plugins.Plugins.UnitedStatesPlugin;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .AddUserSecrets("38200dae-db69-441e-b03a-86f740caac94")
    .Build();

string apiKey = configuration["AzureOpenAI:ApiKey"];
string deploymentName = configuration["AzureOpenAI:DeploymentName"];
string endpoint = configuration["AzureOpenAI:Endpoint"];

Console.WriteLine(apiKey);
Console.WriteLine(deploymentName);

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey)
    .Build();

kernel.ImportPluginFromType<UnitedStatesPlugin>();

//manual function execution
OpenAIPromptExecutionSettings settings = new()
{
    ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
};

string prompt = @"Ask User if  They want to know the population of the United States in 2015.
iF they say yes, then share the population of the United States in 2015 along with their gender
Only consider the last answer of the user
Write a paragraph to share the population of the United States in 2015. 
Make sure to specify how many people, among the population, identify themselves as male and female. 
Don't share approximations, please share the exact numbers.";

var chatHistory = new ChatHistory();
chatHistory.AddMessage(AuthorRole.User, prompt);
while (true)
{
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);

        var functionCalls = ((OpenAIChatMessageContent)result).GetOpenAIFunctionToolCalls();
        foreach (var functionCall in functionCalls)
        {
            Console.WriteLine(functionCall);
            KernelFunction pluginFunction;
            KernelArguments arguments;
            kernel.Plugins.TryGetFunctionAndArguments(functionCall, out pluginFunction, out arguments);
            var functionResult = await kernel.InvokeAsync(pluginFunction!, arguments!);
            var jsonResponse = functionResult.GetValue<object>();
            var json = JsonSerializer.Serialize(jsonResponse);
            Console.WriteLine(json);
            chatHistory.AddMessage(AuthorRole.Tool, json);
        }

        result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, kernel);

        Console.WriteLine(result.Content);

        // automatic function calling

        //OpenAIPromptExecutionSettings settings = new()
        //{
        //    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
        //};

        //var streamingResult = kernel.InvokePromptStreamingAsync(prompt, new KernelArguments(settings));
        //await foreach (var streamingResponse in streamingResult)
        //{
        //    Console.Write(streamingResponse);
        //}

        string userInput = Console.ReadLine();
        chatHistory.AddMessage(AuthorRole.User, userInput);
        chatHistory.AddMessage(AuthorRole.Assistant, result.Content);
}        