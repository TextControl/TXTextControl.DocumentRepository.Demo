using TXTextControl.DocumentRepository.Repositories;
using TXTextControl.Web;
using TXTextControl.Web.DocumentEditor.Backend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<DocumentEditorWorkerManager>();

// Register DocumentRepository
var repositoryPath = Path.Combine(builder.Environment.ContentRootPath, "DocumentRepository");
builder.Services.AddSingleton<IFileDocumentRepository>(sp => 
    new FileDocumentRepository(repositoryPath));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// enable Web Sockets
app.UseWebSockets();

// attach the Text Control WebSocketHandler middleware
app.UseTXWebSocketMiddleware();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
