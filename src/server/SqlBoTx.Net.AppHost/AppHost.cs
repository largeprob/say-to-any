var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddConnectionString("say2any");

var apiService = builder.AddProject<Projects.SqlBoTx_Net_ApiService>("apiservice")
    .WithReference(sql)
    .WithHttpHealthCheck("/health");

var dbManager = builder.AddProject<Projects.SqlBoTx_Net_DbManager>("dbManager")
    .WithReference(sql);

builder.Build().Run();
