using Carter;
using Flownodes.Worker.Bootstrap;

var builder = WebApplication.CreateBuilder(args);
builder.Host.ConfigureOrleans();
builder.Host.ConfigureSerilog();
builder.Services.ConfigureWebServices();

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();

app.Run();