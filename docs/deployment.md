# Deployment

Acme Homes deploys to Azure Container Apps from an Azure DevOps pipeline ([azure-pipelines.yml](../azure-pipelines.yml)). Infrastructure is defined in Bicep under [infra/](../infra/).

## How a deployment works

Every push to `main` runs four stages. Pull requests run only the first.

1. **Build and test.** Builds the solution and runs all tests, including the SQL Server tests (Microsoft-hosted Ubuntu agents have Docker).
2. **Provision.** Deploys [main.bicep](../infra/main.bicep): Log Analytics, the Container Apps environment, the container registry, two managed identities, and Azure SQL. It's idempotent, so unchanged resources are left alone.
3. **Publish.** Builds the API and migrations images with `dotnet publish -t:PublishContainer` (no Dockerfiles) and pushes them to the registry, tagged with the build ID.
4. **Deploy.**
   - Updates the migrations job to the new image, runs it, and waits for it to succeed (see [ADR 0007](decisions/0007-migration-service.md)).
   - Then deploys the new API image and smoke tests `GET /api/markets`.

No secrets are stored anywhere. Azure DevOps signs in to Azure with workload identity federation, and both apps reach the registry and the database through managed identities (see [ADR 0008](decisions/0008-azure-identity-and-database-access.md)).

## One-time setup

You do these steps once. They need the Azure CLI (`winget install -e --id Microsoft.AzureCLI`) and owner rights on your Azure subscription.

### 1. Prepare the subscription

```powershell
az login
az account set --subscription "<your subscription name or ID>"

# New subscriptions often don't have these resource providers registered yet.
foreach ($ns in 'Microsoft.App','Microsoft.ContainerRegistry','Microsoft.Sql','Microsoft.OperationalInsights','Microsoft.ManagedIdentity') {
  az provider register --namespace $ns
}

az group create --name rg-acmehomes-demo --location eastus2
```

### 2. Create the Azure DevOps project

1. Sign in at https://dev.azure.com and create an organization, if you don't have one, and a project named `AcmeHomes`.
2. **Check that you can run pipelines.** New organizations may have no Microsoft-hosted build minutes until Microsoft approves a free grant. If your first run reports "No hosted parallelism has been purchased or granted", request it at https://aka.ms/azpipelines-parallelism-request. Approval can take a few business days, so do this early.

### 3. Create the Azure service connection

1. In the project, go to **Project settings → Service connections → New service connection → Azure Resource Manager**.
2. Choose **Workload identity federation (automatic)**.
3. Set the scope level to **Resource group**: your subscription, then `rg-acmehomes-demo`.
4. Name it `acmehomes-azure` (the name the pipeline expects) and save. Don't grant access to all pipelines; you'll approve it on the first run.

This gives the pipeline **Contributor** on the resource group only.

### 4. Let the pipeline assign the registry pull role

The Bicep template gives each app's identity the `AcrPull` role on the registry. Contributor can't assign roles, so give the service connection's identity **Role Based Access Control Administrator** on the resource group:

1. In the service connection, select **Manage service connection roles** (or **Manage App registration**) and copy its **Application (client) ID**.
2. Run:

```powershell
$rgId = az group show --name rg-acmehomes-demo --query id --output tsv
az role assignment create --assignee "<application client ID>" --role "Role Based Access Control Administrator" --scope $rgId
```

### 5. Create the pipeline

1. Go to **Pipelines → New pipeline → GitHub (YAML)**.
2. Authorize the Azure Pipelines GitHub app for `coreyburnett65/AcmeHomes`.
3. Choose **Existing Azure Pipelines YAML file** and select `/azure-pipelines.yml`.
4. Run it. On the first run, approve the pipeline's access to the `acmehomes-azure` service connection and the `acmehomes-demo` environment when prompted.

The API's URL is printed at the end of the **Deploy API** step and is shown on the container app in the Azure portal. The API reference is at `/scalar`.

## Costs and shutting down between demos

| Resource | Idle cost |
|---|---|
| Container App (API) | Scales to zero after a few idle minutes. Covered by the monthly free allowance. |
| Migrations job | Runs only during deployments. |
| Azure SQL (free offer) | Auto-pauses after an hour idle. 100,000 vCore-seconds a month (about 55 hours of activity at 0.5 vCore); after that it stays paused until next month rather than billing. |
| Log Analytics | First 5 GB a month free; capped at 1 GB a day. |
| Container Registry (Basic) | **About $5/month**, whether used or not. |

Nothing needs to be stopped by hand between demos. The first request after idle takes longer while the API starts and the database resumes.

**To remove all costs**, delete the resource group:

```powershell
az group delete --name rg-acmehomes-demo
```

To bring it back, repeat step 1's `az group create`, recreate the service connection's role assignments (steps 3 and 4, since they were scoped to the deleted group), and rerun the pipeline. Seed data is recreated by the migrations job.

## Connecting to the Azure database yourself

The server allows Microsoft Entra authentication only, and its admin is the migrations identity. To query the database from your own machine, temporarily set yourself as the Entra admin in the portal (**SQL server → Microsoft Entra ID**) and add your IP to the server's firewall. The next pipeline run sets the admin back.
