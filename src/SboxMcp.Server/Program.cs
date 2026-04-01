using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SboxMcp.Server.Bridge;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<EditorBridgeServer>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EditorBridgeServer>());

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
