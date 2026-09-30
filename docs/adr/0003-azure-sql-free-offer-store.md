# Azure SQL Database (free offer) as the store

Trips are relational entities, and this is a cost-sensitive Azure deployment demo. We store them in **Azure SQL Database's free offer** (100,000 vCore-seconds/month, 32 GB, free for the life of the subscription) via EF Core's SQL Server provider, keeping all queries provider-agnostic so SQLite can back fast unit tests and a SQL Server container can back integration tests.

## Considered Options

- **PostgreSQL Flexible Server** — same EF Core support and cheaper than most managed SQL, but ~$12–15/month with no free tier.
- **Cosmos DB** — has a lifetime free tier, but its document model fights the relational entity graph.

## Consequences

- Migrations are provider-specific and must be kept per-provider alongside a shared model.
- The free offer is a billing construct; Terraform's `azurerm_mssql_database` may not expose it, so provisioning may need `azapi` or a one-time portal step (to verify).
- Serverless auto-pause means cold-start latency; acceptable for a demo, not for sustained traffic.
