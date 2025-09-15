using MiniIAM.Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Infra + Auth + EF InMemory + Swagger
builder.AddMiniIamInfrastructure();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.UseMiniIamApi();

app.Run();
