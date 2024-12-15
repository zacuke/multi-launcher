
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
namespace multi_launcher.Launchers;

class SpaLauncher(
    string spaPath, 
    List<string> bindUrls, 
    string spaResponseContentType, 
    string indexHtml,
    Dictionary<string, string> spaResponseHeaders) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {

        var builder = WebApplication.CreateSlimBuilder();

        builder.Services.AddSingleton<IHostLifetime, DisableCtrlCLifeTime>();

        var app = builder.Build();

        foreach (var url in bindUrls)
        {
            Console.WriteLine($"Launching SPA:  {spaPath}, Port: {url}");
            app.Urls.Add(url);
        }

        //serve assets
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(spaPath),
            RequestPath = string.Empty,
        });

        //maps everything to index.html
        app.MapFallback(async context =>
        {
            foreach(var i in spaResponseHeaders)
            {
                context.Response.Headers.Append(i.Key, i.Value);
            }
            context.Response.ContentType = spaResponseContentType;
            await context.Response.SendFileAsync(Path.Combine(spaPath, indexHtml));
        });

        await app.RunAsync(cancellationToken);

    }
}
