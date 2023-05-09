using AventusSharp.Data;
using AventusSharp.Data.Manager.DB;
using AventusSharp.Data.Storage.Default;
using AventusSharp.Data.Storage.Mysql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

MySQLStorage storage = new MySQLStorage(
    new StorageCredentials(
        host: "localhost",
        database: "todo_api",
        username: "maxime",
        password: "pass$1234"
    )
    {
        keepConnectionOpen = true,
    });
await DataMainManager.Register(new DataManagerConfig()
{
    defaultStorage = storage,
    defaultDM = typeof(DatabaseDMSimple<>),
});




app.Run();
