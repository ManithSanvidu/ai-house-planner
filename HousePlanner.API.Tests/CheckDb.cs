using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HousePlanner.API.Data;
using Microsoft.Extensions.DependencyInjection;

public class CheckDb {
    public static void Run() {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql("Host=localhost;Database=ai-house-planner;Username=postgres;Password=postgres"));
        var sp = services.BuildServiceProvider();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var project = db.Projects.FirstOrDefault(p => p.Id == Guid.Parse("921f08e5-0c13-4641-b0c3-bf743df0994f"));
        Console.WriteLine($"Project found: {project != null}");
    }
}
