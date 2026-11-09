using k8s.Models;

namespace Provisioner.Service.Provisioning.Manifests;

public static class AppManifests
{
    // TODO: (SECURITY) Make more secure by possibly hashing and atleased adding customerId to the secret name
    public static string SecretName(string appName) => $"{appName}-secrets"; 

    private static Dictionary<string, string> Labels(string appName) => new() { ["app"] = appName };

    public static V1Secret Secret(string appName, WorkloadSpec workload) => new()
    {
        Metadata = new V1ObjectMeta { Name = SecretName(appName) },
        StringData = workload.Secrets.ToDictionary()
    };

    public static V1Deployment Deployment(string appName, WorkloadSpec workload)
    {
        var plainEnv = workload.Env
            .Select(e => new V1EnvVar { Name = e.Key, Value = e.Value });

        var secretEnv = workload.Secrets.Keys
            .Select(key => new V1EnvVar
            {
                Name = key,
                ValueFrom = new V1EnvVarSource
                {
                    SecretKeyRef = new V1SecretKeySelector { Name = SecretName(appName), Key = key }
                }
            });

        return new V1Deployment
        {
            Metadata = new V1ObjectMeta { Name = appName, Labels = Labels(appName) },
            Spec = new V1DeploymentSpec
            {
                Replicas = workload.Replicas,
                Selector = new V1LabelSelector { MatchLabels = Labels(appName) },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta { Labels = Labels(appName) },
                    Spec = new V1PodSpec
                    {
                        Containers =
                        [
                            new V1Container
                            {
                                Name = "app",
                                Image = workload.Image,
                                Env = plainEnv.Concat(secretEnv).ToList()
                            }
                        ]
                    }
                }
            }
        };
    }

    public static V1Service Service(string appName, ServiceSpec service) => new()
    {
        Metadata = new V1ObjectMeta { Name = appName },
        Spec = new V1ServiceSpec
        {
            Selector = Labels(appName),
            Ports = [new V1ServicePort { Port = service.Port, TargetPort = service.TargetPort }]
        }
    };

    public static V1Ingress Ingress(string appName, string host, ServiceSpec service) => new()
    {
        Metadata = new V1ObjectMeta { Name = appName },
        Spec = new V1IngressSpec
        {
            Rules =
            [
                new V1IngressRule
                {
                    Host = host,
                    Http = new V1HTTPIngressRuleValue
                    {
                        Paths =
                        [
                            new V1HTTPIngressPath
                            {
                                Path = "/",
                                PathType = "Prefix",
                                Backend = new V1IngressBackend
                                {
                                    Service = new V1IngressServiceBackend
                                    {
                                        Name = appName,
                                        Port = new V1ServiceBackendPort { Number = service.Port }
                                    }
                                }
                            }
                        ]
                    }
                }
            ]
        }
    };
}