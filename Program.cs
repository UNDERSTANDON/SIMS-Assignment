using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication;
using SIMS_Assignment.Authentication.SecurityHasher;
using SIMS_Assignment.Storage;
using SIMS_Assignment.Services;
using SIMS_WEB.Storage;

var builder = WebApplication.CreateBuilder(args);

// Unify all DataStorage paths to use the project root (ContentRootPath) so data
// files are not duplicated in bin/Debug/net9.0/DataStorage.
var dataStoragePath = Path.Combine(builder.Environment.ContentRootPath, "DataStorage");
ModelFilePersistence.DataDir = dataStoragePath;

// Ensure chosen ports are available; if not, pick free ports to avoid hard crash when default ports are in use.
static bool PortAvailable(int port)
{
    try
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch { return false; }
}

static int GetFreePort()
{
    var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
    listener.Start();
    var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
    listener.Stop();
    return port;
}

// Default ports used by the templates; if they are busy, select alternatives and instruct Kestrel to use them.
int desiredHttp = 5126;
int desiredHttps = 7235;
if (!PortAvailable(desiredHttp))
{
    var alt = GetFreePort();
    Console.WriteLine($"Port {desiredHttp} is in use, falling back to {alt} for HTTP.");
    desiredHttp = alt;
}
if (!PortAvailable(desiredHttps))
{
    var alt = GetFreePort();
    Console.WriteLine($"Port {desiredHttps} is in use, falling back to {alt} for HTTPS.");
    desiredHttps = alt;
}

builder.WebHost.UseUrls($"http://127.0.0.1:{desiredHttp}", $"https://127.0.0.1:{desiredHttps}");

// Add services to the container.
builder.Services.AddControllersWithViews();
// Register CSV-backed storage and password hasher
// Store CSV files under the content root in a `DataStorage` folder
builder.Services.AddSingleton<IDataStorage>(sp =>
    new CvsStorageEngine(dataStoragePath));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<IAuth, AuthFacade>();
// Course-related handlers for materials, assignments, submissions
builder.Services.AddSingleton<SIMS_Assignment.Services.CourseServices.MaterialHandler>();
builder.Services.AddSingleton<SIMS_Assignment.Services.CourseServices.AssignmentHandler>();
builder.Services.AddSingleton<SIMS_Assignment.Services.CourseServices.SubmissionHandler>();
// Student manager service (backend migration)
// Backend managers
builder.Services.AddScoped<IStudentManager, StudentManager>();
builder.Services.AddScoped<ICourseManager, CourseManager>();
builder.Services.AddScoped<IEnrollmentManager, EnrollmentManager>();
// Startup initializer to ensure CSV files exist on first run
builder.Services.AddSingleton<SIMS_WEB.Storage.StartupDataInitializer>();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Initialize persistence files before the app starts handling requests
try
{
    var initializer = app.Services.GetRequiredService<SIMS_WEB.Storage.StartupDataInitializer>();
    initializer.Initialize();
}
catch (Exception ex)
{
    // Log the initialization error so it's visible during startup
    Console.WriteLine($"Startup data initialization failed: {ex}");
}

// Global exception handlers to capture unexpected crashes during runtime
void LogUnhandled(Exception? ex, string? source = null)
{
    try
    {
        var dataDir = dataStoragePath;
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        var path = Path.Combine(dataDir, "last_error.txt");
        var text = $"[{DateTime.Now:O}] Unhandled ({source})\n{ex}\n\n";
        File.AppendAllText(path, text);
        Console.WriteLine(text);
    }
    catch { }
}

AppDomain.CurrentDomain.UnhandledException += (s, e) =>
{
    LogUnhandled(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
};

// First-chance exceptions are raised when the runtime first encounters an exception.
// Recording them helps diagnose crashes that may be swallowed or escalate to native failures.
AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
{
    try
    {
        var dataDir = dataStoragePath;
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        var path = Path.Combine(dataDir, "first_chance.txt");
        var text = $"[{DateTime.Now:O}] FirstChance: {e.Exception}\n\n";
        File.AppendAllText(path, text);
    }
    catch { }
};

// Log when the process is exiting so we can see abrupt terminations that don't raise managed exceptions
AppDomain.CurrentDomain.ProcessExit += (s, e) =>
{
    try
    {
        var dataDir = dataStoragePath;
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        var path = Path.Combine(dataDir, "process_exit_log.txt");
        var text = $"[{DateTime.Now:O}] ProcessExit. Environment: OS={Environment.OSVersion}, PID={Environment.ProcessId}\n";
        File.AppendAllText(path, text);
    }
    catch { }
};

TaskScheduler.UnobservedTaskException += (s, e) =>
{
    LogUnhandled(e.Exception, "TaskScheduler.UnobservedTaskException");
    e.SetObserved();
};

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
// Log every incoming request to help determine whether browser requests reach the server
app.Use(async (context, next) =>
{
    try
    {
        var dataDir = dataStoragePath;
        if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        var path = Path.Combine(dataDir, "request_log.txt");
        var info = $"[{DateTime.Now:O}] {context.Request.Method} {context.Request.Path} Content-Length:{context.Request.ContentLength} Remote:{context.Connection.RemoteIpAddress}\n";
        File.AppendAllText(path, info);
    }
    catch { }
    await next();
});
// Exception-catching middleware placed early in the pipeline to capture errors that occur
// during request processing (including model binding) so they don't terminate the process
// before controller breakpoints are hit. This will log the exception via the existing
// LogUnhandled helper and return a 500 response.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        try
        {
            LogUnhandled(ex, "Middleware.UnhandledException");
        }
        catch { }
        // Ensure we don't rethrow; return a 500 page to the client.
        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("An internal error occurred. The error has been logged.");
        }
    }
});
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

try
{
    app.Run();
}
catch (Exception ex)
{
    LogUnhandled(ex, "App.Run");
    // keep console open for debugging when running from VS
    Console.ReadKey();
    throw;
}

public partial class Program { }

