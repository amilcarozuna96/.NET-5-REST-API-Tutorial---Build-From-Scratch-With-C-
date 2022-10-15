using Catalog.Repositories;
using MongoDB.Bson.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options => {
    options.SuppressAsyncSuffixInActionNames = false;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Aquí puse todo lo que el intructor ponia en el archivo de Startup.cs que no me generó VSC
BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));


builder.Services.AddSingleton<IMongoClient>(serviceProvider => {
    var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>()
    return new MongoClient(MongoDbSettings.ConnectionString);

});

builder.Services.AddSingleton<IItemsRepository, InMemItemsRepository>();
builder.Services.AddHealthChecks().AddMongoDb(mongoDbsettings.ConnectionString, name: "mongodb", timeout: TimeSpan.FromSeconds(3),
tags: new[]{"ready"});

app.UseEndPoints(endpoints =>{
    endpoints.MapControllers();
    endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions{
        Predicate = (check) => check.Tags.Contains("ready"),
        ResponseWritter = async(context, report) => {
            var result = JsonSerializer.Serialize(new {
                status = report.Status.ToString(),
                checks = report.Entries.Select(entry => new {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    exception = entry.Value.Exception != null ? entry.Value.Exception.Message : "none",
                    duration = entry.Value.Duration.ToString()
                })
            });

            context.Response.ContentType = mediaTypeNames.Application.Json;
            await context.Response.WriteAsync(result);
        }
    });

    endpoints.MapHealthChecks("/health/live", new HealthCheckOptions{
        Predicate = () => false
    });
});

//Hasta aquí...

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
