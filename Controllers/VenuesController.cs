using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EventEase.Data;
using EventEase.Models;
using EventEase.Services;

namespace EventEase.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly BlobStorageService _blobStorageService;

        public VenuesController(ApplicationDbContext context, BlobStorageService blobStorageService)
        {
            _context = context;
            _blobStorageService = blobStorageService;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            var venues = _context.Venues.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var search = searchString.Trim();
                venues = venues.Where(v =>
                    v.VenueName.Contains(search) ||
                    v.Location.Contains(search) ||
                    v.Capacity.ToString().Contains(search));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await venues.OrderBy(v => v.VenueName).ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Venue venue, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(venue);
            }

            try
            {
                venue.ImageUrl = await _blobStorageService.UploadAsync(imageFile) ?? venue.ImageUrl;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(Venue.ImageUrl), ex.Message);
                return View(venue);
            }
            catch
            {
                ModelState.AddModelError(nameof(Venue.ImageUrl), "Image upload failed. Please make sure Azurite is running and try again.");
                return View(venue);
            }

            _context.Venues.Add(venue);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue saved successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Venue venue, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(venue);
            }

            try
            {
                venue.ImageUrl = await _blobStorageService.UploadAsync(imageFile) ?? venue.ImageUrl;
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(Venue.ImageUrl), ex.Message);
                return View(venue);
            }
            catch
            {
                ModelState.AddModelError(nameof(Venue.ImageUrl), "Image upload failed. Please make sure Azurite is running and try again.");
                return View(venue);
            }

            _context.Venues.Update(venue);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            var hasEvents = await _context.Events.AnyAsync(e => e.VenueId == id);
            var hasBookings = await _context.Bookings.AnyAsync(b => b.VenueId == id);

            if (hasEvents || hasBookings)
            {
                TempData["ErrorMessage"] = "This venue cannot be deleted because it has linked events or active bookings.";
                return RedirectToAction(nameof(Index));
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Venue deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null) return NotFound();

            return View(venue);
        }
    }
}
