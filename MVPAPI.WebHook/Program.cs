using MVPAPI.WebHook.Application;
using MVPAPI.WebHook.Application.Common;
using MVPAPI.WebHook.Application.Interfaces.Services;
using MVPAPI.WebHook.Application.Services;
using MVPAPI.WebHook.BackgroundServices;
using MVPAPI.WebHook.Infrastructure;
using MVPAPI.WebHook.Middleware;
using MVPAPI.WebHook.OpenApi;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options => options.AddPolicy("DevCors", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
}

builder.Services.AddSingleton<XmlCommentsOperationTransformer>();
builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<XmlCommentsOperationTransformer>();
});

builder.Services.Configure<WebhookDispatchOptions>(
    builder.Configuration.GetSection(WebhookDispatchOptions.SectionName));
builder.Services.Configure<MVPApiOptions>(
    builder.Configuration.GetSection(MVPApiOptions.SectionName));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<WebhookDispatcherService>();
builder.Services.AddHostedService<OutboxProcessorService>();

builder.Services.AddHttpClient<IAccountApiClient, AccountApiClient>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();

        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "MVPAPI WebHook v1");
    });
    app.UseCors("DevCors");
}

app.UseStaticFiles();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
