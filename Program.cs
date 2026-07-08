using SIMS_Assignment.Abstract;
using SIMS_Assignment.Authentication.SecurityHasher;
using SIMS_Assignment.Storage;
using SIMS_Assignment.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Register CSV-backed storage and password hasher
// Store CSV files under the application base directory in a `DataStorage` folder
// (use AppContext.BaseDirectory so the startup initializer and storage engine use the same path)
builder.Services.AddSingleton<IDataStorage>(sp =>
    new CvsStorageEngine(Path.Combine(AppContext.BaseDirectory, "DataStorage")));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
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
