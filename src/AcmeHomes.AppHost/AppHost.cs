var builder = DistributedApplication.CreateBuilder(args);

// Local SQL Server in a container. Persistent lifetime keeps it running between AppHost runs, so startup is fast,
// and the data volume keeps the database if the container is recreated. The generated password is stored in
// this project's user secrets.
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

// The resource name becomes the connection string name the services read (AcmeHomesDbContext.ConnectionName).
var database = sql.AddDatabase("acmehomes");

// Applies EF Core migrations and seed data, then exits.
var migrations = builder.AddProject<Projects.AcmeHomes_MigrationService>("migrations")
    .WithReference(database)
    .WaitFor(database);

// The API only starts after migrations finish successfully, so it never sees an out-of-date schema.
builder.AddProject<Projects.AcmeHomes_Api>("api")
    .WithReference(database)
    .WaitForCompletion(migrations);

builder.Build().Run();
