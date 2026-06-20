# Kubernetes manifests

Kustomize-organized manifests for the whole application platform: 5 services, the API gateway, 5 PostgreSQL databases, RabbitMQ, and Keycloak (43 resources).

```
k8s/
├── kustomization.yaml        # ties everything together (+ generates the Keycloak realm ConfigMap)
├── namespace.yaml            # marketplace namespace
├── ingress.yaml              # external access (gateway + keycloak)
├── catalog/                  # ConfigMap + Secret + Deployment + Service (split, documented example)
├── inventory/  | notification/ | payment/ | order/ | gateway/   # one consolidated manifest each
└── infra/
    ├── databases.yaml        # 5× PostgreSQL StatefulSet + headless Service + shared Secret
    ├── rabbitmq.yaml         # MassTransit broker
    └── keycloak.yaml         # OIDC provider (realm imported from docker/keycloak/)
```

## Concepts demonstrated
- **Deployment** for stateless services, **StatefulSet + PVC** for databases (stable identity + persistent disk).
- **ConfigMap / Secret** split — non-secret config vs. passwords/connection strings.
- **Service** (ClusterIP) for in-cluster discovery; **headless Service** for the stateful databases.
- **liveness / readiness / startup probes** wired to the apps' existing `/health/live` and `/health/ready` endpoints.
- **Ingress** for external HTTP entry through the gateway.
- **kustomize** as the single apply unit, incl. `configMapGenerator` for the Keycloak realm (no file duplication).

## Validate without a cluster
```bash
kubectl kustomize k8s/ --load-restrictor LoadRestrictionsNone
```

## Deploy to a local cluster
Prerequisites: a cluster (kind / minikube / Docker Desktop Kubernetes) and, for the Ingress, an [ingress-nginx](https://kubernetes.github.io/ingress-nginx/) controller.

```bash
# 1) Build images and make them available to the cluster.
#    Deployments reference ghcr.io/marketplace/<svc>:latest — either push those via CI,
#    or for kind, build locally and load:
docker compose build
kind load docker-image marketplaceorderplatform-catalog-api:latest   # ...per service
#    (adjust the image: fields, or use a kustomize `images:` transformer to remap.)

# 2) Apply everything.
kubectl apply -k k8s/ --load-restrictor LoadRestrictionsNone

# 3) Watch it come up.
kubectl get pods -n marketplace -w

# 4) Reach the gateway (either port-forward or via Ingress).
kubectl port-forward -n marketplace svc/api-gateway 8085:8080
#    or add to /etc/hosts:  127.0.0.1 marketplace.localhost keycloak.localhost
```

## Notes
- All credentials are local-development defaults. In production, source Secrets from a secret manager (Sealed Secrets / External Secrets) rather than committing them.
- The Keycloak `ValidIssuer` must match the issuer in tokens Keycloak mints; when exposing Keycloak under a different hostname, set its frontend URL accordingly.
- **Observability stack** (Prometheus / Grafana / Tempo / OTel Collector / Elasticsearch) currently ships via `docker compose`. A Kubernetes overlay for it can be added as a follow-up; the app manifests already export OTLP traces and `/metrics`.
