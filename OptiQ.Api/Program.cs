var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "OptiQ API", 
        Version = "v1",
        Description = "Quantum-powered portfolio optimization API using QAOA"
    });
});

// Add CORS for Blazor UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorUI", policy =>
    {
        policy.WithOrigins("https://localhost:7000", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OptiQ API v1");
        c.RoutePrefix = string.Empty; // Make Swagger the root page
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorUI");
app.UseAuthorization();
app.MapControllers();

app.Run();
