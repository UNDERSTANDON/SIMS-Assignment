using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication.SecurityHasher;
using SIMS_Assignment.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Register CSV-backed storage and password hasher
// Store CSV files under the application content root in a `DataStorage` folder
builder.Services.AddSingleton<IDataStorage>(sp =>
    new CvsStorageEngine(Path.Combine(builder.Environment.ContentRootPath, "DataStorage")));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
// Course-related handlers for materials, assignments, submissions
builder.Services.AddSingleton<SIMS_Assignment.Services.CourseServices.MaterialHandler>();
builder.Services.AddSingleton<SIMS_Assignment.Services.CourseServices.AssignmentHandler>();
builder.Services.AddSingleton<SIMS_Assignment.Services.CourseServices.SubmissionHandler>();
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
catch { }

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
