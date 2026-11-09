using k8s.Models;

namespace Provisioner.Service.Provisioning.Manifests;

public static class CustomerManifests
{
    public static V1Namespace Namespace(string ns, string customerId) => new()
    {
        Metadata = new V1ObjectMeta
        {
            Name = ns,
            Labels = new Dictionary<string, string>
            {
                ["paas/managed"] = "true",
                ["paas/customer-id"] = customerId
            }
        }
    };

    public static V1NetworkPolicy DefaultDenyIngress() => new()
    {
        Metadata = new V1ObjectMeta { Name = "default-deny-ingress" },
        Spec = new V1NetworkPolicySpec
        {
            PodSelector = new V1LabelSelector(),
            PolicyTypes = ["Ingress"]
        }
    };

    public static V1NetworkPolicy AllowNamespaceAndIngressController() => new()
    {
        Metadata = new V1ObjectMeta { Name = "allow-namespace-and-ingress" },
        Spec = new V1NetworkPolicySpec
        {
            PodSelector = new V1LabelSelector(),
            PolicyTypes = ["Ingress"],
            Ingress =
            [
                new V1NetworkPolicyIngressRule
                {
                    FromProperty =
                    [
                        new V1NetworkPolicyPeer { PodSelector = new V1LabelSelector() },
                        new V1NetworkPolicyPeer
                        {
                            NamespaceSelector = new V1LabelSelector
                            {
                                MatchLabels = new Dictionary<string, string>
                                {
                                    ["kubernetes.io/metadata.name"] = "kube-system"
                                }
                            },
                            PodSelector = new V1LabelSelector
                            {
                                MatchLabels = new Dictionary<string, string>
                                {
                                    ["app.kubernetes.io/name"] = "traefik"
                                }
                            }
                        }
                    ]
                }
            ]
        }
    };
}