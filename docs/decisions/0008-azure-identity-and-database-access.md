# 0008: Azure identities and database access without secrets

**Status:** Accepted (2026-11-02)

## Context

The deployed API and migrations job need to pull images from the container registry and connect to Azure SQL. The project rule is that no secrets are committed or stored in the pipeline. Azure costs must stay near zero.

## Decision

- **Pipeline to Azure:** an Azure DevOps service connection with workload identity federation. It uses short-lived tokens and has no client secret to store or rotate, and it's scoped to the demo resource group.
- **Apps to registry:** each app has a user-assigned managed identity with `AcrPull`. The registry's admin account is disabled.
- **Apps to database:** Azure SQL uses Microsoft Entra authentication only, so no SQL logins or passwords exist. Connection strings name the identity's client ID and contain nothing secret.
  - The **migrations identity** is the server's Entra admin. It applies migrations and creates the API's database user.
  - The **API identity** is a database user with `db_datareader` only. The migrations job creates it on every run, idempotently, using `CREATE USER ... WITH SID = <client ID>, TYPE = E`. That syntax avoids giving the SQL server Microsoft Graph permissions to look the identity up by name.
- **Network:** the SQL firewall allows Azure services. Consumption-plan Container Apps have no fixed outbound IPs, and every connection still has to authenticate with Entra.

## Consequences

- The migrations identity has server-admin rights, more than migrations alone need. It's acceptable here because that identity already owns the schema and only runs during deployments. In production, the Entra admin would be a group, and user provisioning would run separately with its own identity.
- Developers can't connect to the Azure database without temporarily changing the Entra admin (see docs/deployment.md).
- Health probes use `/alive`, which has no database check, so probes never keep the serverless database awake.

## Alternatives considered

- **SQL login with the password in Key Vault:** simpler setup, but it adds a secret to rotate and a resource to manage.
- **Pipeline runs the user-creation script:** needs the build agent's IP opened on the SQL firewall and a SQL command-line tool installed on every run. Running it inside the migrations job avoids both.
- **Private endpoint and VNet integration for SQL:** removes public network access entirely, but adds fixed monthly networking costs the demo doesn't justify.
