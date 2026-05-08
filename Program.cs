using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<BlobStorageService>();

// DB
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.EventTypes.Any(et => et.TypeName == "Wedding"))
    {
        context.EventTypes.Add(new EventType { TypeName = "Wedding", Description = "Wedding ceremonies and receptions" });
    }

    if (!context.EventTypes.Any(et => et.TypeName == "Conference"))
    {
        context.EventTypes.Add(new EventType { TypeName = "Conference", Description = "Business conferences and seminars" });
    }

    if (!context.EventTypes.Any(et => et.TypeName == "Birthday"))
    {
        context.EventTypes.Add(new EventType { TypeName = "Birthday", Description = "Birthday parties and celebrations" });
    }

    if (!context.EventTypes.Any(et => et.TypeName == "Concert"))
    {
        context.EventTypes.Add(new EventType { TypeName = "Concert", Description = "Music and live performance events" });
    }

    if (!context.EventTypes.Any(et => et.TypeName == "Workshop"))
    {
        context.EventTypes.Add(new EventType { TypeName = "Workshop", Description = "Training sessions and workshops" });
    }

    if (!context.Venues.Any(v => v.VenueName == "Grand Hall"))
    {
        context.Venues.Add(new Venue
        {
            VenueName = "Grand Hall",
            Location = "Johannesburg",
            Capacity = 300,
            Description = "Large indoor venue for weddings, conferences, and formal events."
        });
    }

    if (!context.Venues.Any(v => v.VenueName == "Garden Pavilion"))
    {
        context.Venues.Add(new Venue
        {
            VenueName = "Garden Pavilion",
            Location = "Pretoria",
            Capacity = 120,
            Description = "Outdoor venue suited for birthdays, receptions, and smaller events."
        });
    }

    if (!context.Venues.Any(v => v.VenueName == "City Conference Room"))
    {
        context.Venues.Add(new Venue
        {
            VenueName = "City Conference Room",
            Location = "Sandton",
            Capacity = 80,
            Description = "Professional venue for meetings, workshops, and business events."
        });
    }

    context.SaveChanges();

    if (!context.Events.Any())
    {
        var grandHall = context.Venues.First(v => v.VenueName == "Grand Hall");
        var gardenPavilion = context.Venues.First(v => v.VenueName == "Garden Pavilion");
        var conferenceRoom = context.Venues.First(v => v.VenueName == "City Conference Room");

        var wedding = context.EventTypes.First(et => et.TypeName == "Wedding");
        var birthday = context.EventTypes.First(et => et.TypeName == "Birthday");
        var workshop = context.EventTypes.First(et => et.TypeName == "Workshop");

        context.Events.AddRange(
            new Event
            {
                EventName = "Smith Wedding",
                StartDate = DateTime.Today.AddDays(7).AddHours(14),
                EndDate = DateTime.Today.AddDays(7).AddHours(22),
                VenueId = grandHall.VenueId,
                EventTypeId = wedding.EventTypeId,
                Description = "Wedding ceremony and evening reception."
            },
            new Event
            {
                EventName = "Birthday Celebration",
                StartDate = DateTime.Today.AddDays(10).AddHours(12),
                EndDate = DateTime.Today.AddDays(10).AddHours(17),
                VenueId = gardenPavilion.VenueId,
                EventTypeId = birthday.EventTypeId,
                Description = "Private birthday event."
            },
            new Event
            {
                EventName = "Cloud Skills Workshop",
                StartDate = DateTime.Today.AddDays(14).AddHours(9),
                EndDate = DateTime.Today.AddDays(14).AddHours(15),
                VenueId = conferenceRoom.VenueId,
                EventTypeId = workshop.EventTypeId,
                Description = "Training workshop for cloud development."
            }
        );

        context.SaveChanges();
    }
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

