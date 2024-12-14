
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
namespace multi_launcher;

class SpaLauncher : BackgroundService
{

    private readonly string _spaPath;
    private readonly string _bindUrl;
    public SpaLauncher(string spaPath, string bindUrl)
    {
        _spaPath = spaPath;
        _bindUrl = bindUrl;
    }
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {

        var builder = WebApplication.CreateSlimBuilder(); 

        builder.Services.AddSingleton<IHostLifetime, DisableCtrlCLifeTime>();

        var app = builder.Build();

        app.Urls.Add(_bindUrl);

        //serve assets
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(_spaPath),
            RequestPath = string.Empty,
        });

        //maps everything to index.html
        app.MapFallback(async context =>
        {
            //todo: allow configuring for prod
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "*");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "*");
            context.Response.ContentType = "text/html";
            await context.Response.SendFileAsync(Path.Combine(_spaPath, "index.html"));
        });

        await app.RunAsync(cancellationToken);

    }
}
