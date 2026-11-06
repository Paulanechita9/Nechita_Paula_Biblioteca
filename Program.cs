using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Intalnirea1.Data;
using Intalnirea1.Models;
using Microsoft.Extensions.ML;
using Intalnirea1.Api;
using Scalar.AspNetCore;

using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<Intalnirea1.Services.RecommenderTrainer>();
builder.Services.AddScoped<Intalnirea1.Services.GenreClassifier>();
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LibraryContext") ?? throw new InvalidOperationException("Connection string 'LibraryContext' not found.")));

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services
    .AddPredictionEnginePool<BookRating, BookRatingPrediction>()
    .FromFile(modelName: "BookRecommender",
              filePath: "MLModels/BookRecommender.zip",
              watchForChanges: true);


var endpoint = new Uri("http://localhost:11434/v1/");

IChatClient chat = new OpenAIClient(
    new ApiKeyCredential("ollama"),
    new OpenAIClientOptions { Endpoint = endpoint })
    .GetChatClient("llama3.2")
    .AsIChatClient();

builder.Services.AddSingleton<IChatClient>(chat);

builder.Services.AddValidation();
builder.Services.AddOpenApi();
var app = builder.Build();



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // /openapi/v1.json (OpenAPI 3.1 în .NET 10)
    app.MapScalarApiReference();    // /scalar
}

app.MapLibraryApi();

app.MapPost("/api/v2/books/{id:int}/pitch", async (int id, LibraryContext db, IChatClient ai) =>
{
    var b = await db.Book.Include(x => x.Author).Include(x => x.Genre)
                         .FirstOrDefaultAsync(x => x.ID == id);
    if (b is null) return Results.NotFound();

    List<ChatMessage> messages =
    [
        new(ChatRole.System, "Ești bibliotecar. Răspunde în română, în cel mult 2 propoziții."),
        new(ChatRole.User, $"Recomandă „{b.Title}” de {b.Author?.FullName} " +
                           $"({b.Genre?.Name}). ")
    ];
    ChatResponse r = await ai.GetResponseAsync(messages);
    return Results.Ok(new { b.Title, Pitch = r.Text });
});

app.MapGet("/api/v2/ask", async (string q, IChatClient ai) =>
    (await ai.GetResponseAsync(q)).Text);



using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryContext>();
    db.Database.Migrate();          // aplică migrările (înlocuiește Update-Database)
    LibrarySeed.Seed(db);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// Prețurile folosesc punctul zecimal (45.50), indiferent de setările regionale ale sistemului.
app.UseRequestLocalization("en-US");

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "start",
    pattern: "start",
    defaults: new { controller = "Home", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
