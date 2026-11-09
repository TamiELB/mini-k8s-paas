using k8s;
using MassTransit;
using Microsoft.Extensions.Options;
using Provisioner.Service.Consumers;
using Provisioner.Service.Options;
using Provisioner.Service.Provisioning;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddOptions<RabbitMqOptions>()
    .BindConfiguration(RabbitMqOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IKubernetes>(sp =>
{
    var config = KubernetesClientConfiguration.IsInCluster() ? KubernetesClientConfiguration.InClusterConfig() : KubernetesClientConfiguration.BuildConfigFromConfigFile();
    return new Kubernetes(config);
});

builder.Services.AddSingleton<IKubernetesProvisioner, KubernetesProvisioner>();
builder.Services.AddSingleton<KubernetesApplier>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<AppRequestedConsumer>();

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

var host = builder.Build();
host.Run();
