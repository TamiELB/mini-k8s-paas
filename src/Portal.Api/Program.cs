using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Portal.Api.Consumers;
using Portal.Api.Endpoints;
using Portal.Api.Persistence;
using Portal.Api.Service.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddDbContext<PortalDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Portal")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProvisioningFailedConsumer>();
    x.AddConsumer<AppProvisionedConsumer>();

    x.AddEntityFrameworkOutbox<PortalDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbit = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        cfg.Host(rabbit.Host, rabbit.VirtualHost, h =>
        {
            h.Username(rabbit.Username);
            h.Password(rabbit.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapAppEndpoints();

app.UseHttpsRedirection();

app.Run();