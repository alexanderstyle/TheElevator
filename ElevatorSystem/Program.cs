using ElevatorSystem.Services;

namespace ElevatorSystem;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                policy => policy
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        });

        // Add services to the container.
        builder.Services.AddControllersWithViews();

        builder.Services.AddSingleton<ElevatorManager>();

        // We need a background service to run our elevator simulation logic.
        builder.Services.AddHostedService<ElevatorSimulationService>();
        // We need a background service to run our simulated elevator requests from along the hall.
        builder.Services.AddHostedService<ElevatorHallRequestGenerator>();

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

        app.UseAuthorization();

        app.MapStaticAssets();
        app.UseCors("AllowAllOrigins");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        app.Run();
    }
}
