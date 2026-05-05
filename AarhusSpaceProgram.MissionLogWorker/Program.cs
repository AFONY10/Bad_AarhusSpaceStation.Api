using AarhusSpaceProgram.MissionLogWorker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<MissionLogWorkerOptions>(
    builder.Configuration.GetSection(MissionLogWorkerOptions.SectionName));
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<MissionLogApiClient>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
