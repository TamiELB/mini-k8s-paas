using Contracts;

namespace Portal.Api.Endpoints.Dtos;

public static class CreateAppRequestMapping
{
    public static AppRequested ToAppRequested(this CreateAppRequest request, Guid appId)
    {
        return new AppRequested(
            AppId: appId,
            CustomerId: request.CustomerId,
            AppName: request.AppName,
            Workload: new WorkloadRequest(
                Image: request.Workload.Image,
                Replicas: request.Workload.Replicas,
                Env: request.Workload.Env,
                Secrets: request.Workload.Secrets),
            Service: request.Service is not null
                ? new ServiceRequest(
                    Port: request.Service.Port,
                    TargetPort: request.Service.TargetPort)
                : null,
            Ingress: request.Ingress is not null
                ? new IngressRequest(
                    Subdomain: request.Ingress.Subdomain)
                : null);
    }
}