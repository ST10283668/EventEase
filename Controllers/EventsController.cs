using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventEase.Data;
using EventEase.Models;
using EventEase.Services;

namespace EventEase.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public EventsController(ApplicationDbContext context, BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            var events = _context.Events
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var search = searchString.Trim();
                events = events.Where(e =>
                    e.EventName.Contains(search) ||
                    (e.Venue != null && e.Venue.VenueName.Contains(search)) ||
                    (e.EventType != null && e.EventType.TypeName.Contains(search)));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await events.OrderBy(e => e.StartDate).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await PopulateSelectListsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event ev, IFormFile? imageFile)
        {
            if (ev.EndDate <= ev.StartDate)
            {
                ModelState.AddModelError(nameof(Event.EndDate), "End date must be after the start date.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(ev.VenueId, ev.EventTypeId);
                return View(ev);
            }

            try
            {
                ev.ImageUrl = await _blobStorageService.UploadAsync(imageFile) ?? ev.ImageUrl;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(Event.ImageUrl), ex.Message);
                await PopulateSelectListsAsync(ev.VenueId, ev.EventTypeId);
                return View(ev);
            }
            catch
            {
                ModelState.AddModelError(nameof(Event.ImageUrl), "Image upload failed. Please make sure Azurite is running and try again.");
                await PopulateSelectListsAsync(ev.VenueId, ev.EventTypeId);
                return View(ev);
            }

            _context.Events.Add(ev);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event saved successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();

            return View(ev);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            await PopulateSelectListsAsync(ev.VenueId, ev.EventTypeId);
            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Event ev, IFormFile? imageFile)
        {
            if (ev.EndDate <= ev.StartDate)
            {
                ModelState.AddModelError(nameof(Event.EndDate), "End date must be after the start date.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(ev.VenueId, ev.EventTypeId);
                return View(ev);
            }

            try
            {
                ev.ImageUrl = await _blobStorageService.UploadAsync(imageFile) ?? ev.ImageUrl;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(Event.ImageUrl), ex.Message);
                await PopulateSelectListsAsync(ev.VenueId, ev.EventTypeId);
                return View(ev);
            }
            catch
            {
                ModelState.AddModelError(nameof(Event.ImageUrl), "Image upload failed. Please make sure Azurite is running and try again.");
                await PopulateSelectListsAsync(ev.VenueId, ev.EventTypeId);
                return View(ev);
            }

            _context.Events.Update(ev);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (ev == null) return NotFound();

            return View(ev);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _context.Events.FindAsync(id);
            if (ev == null) return NotFound();

            var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = "This event cannot be deleted because it has active bookings.";
                return RedirectToAction(nameof(Index));
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Event deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectListsAsync(int? venueId = null, int? eventTypeId = null)
        {
            var venues = await _context.Venues.OrderBy(v => v.VenueName).ToListAsync();
            var eventTypes = await _context.EventTypes.OrderBy(et => et.TypeName).ToListAsync();

            ViewBag.HasVenues = venues.Any();
            ViewBag.HasEventTypes = eventTypes.Any();
            ViewBag.Venues = new SelectList(venues, "VenueId", "VenueName", venueId);
            ViewBag.EventTypes = new SelectList(eventTypes, "EventTypeId", "TypeName", eventTypeId);
        }
    }
}
