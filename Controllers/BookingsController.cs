using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EventEase.Data;
using EventEase.Models;

namespace EventEase.Controllers
{
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? searchString)
        {
            var bookings = _context.Bookings
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var search = searchString.Trim();
                bookings = bookings.Where(b =>
                    b.BookingId.ToString().Contains(search) ||
                    b.BookingReference.Contains(search) ||
                    (b.Event != null && b.Event.EventName.Contains(search)) ||
                    (b.Venue != null && b.Venue.VenueName.Contains(search)));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await bookings.OrderBy(b => b.StartDate).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await PopulateSelectListsAsync();
            return View(new Booking
            {
                BookingDate = DateTime.Now,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                Status = "Confirmed"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            await ValidateBookingAsync(booking);

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }

            if (string.IsNullOrWhiteSpace(booking.BookingReference))
            {
                booking.BookingReference = $"BKG-{DateTime.UtcNow:yyyyMMddHHmmss}";
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Booking saved successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            await PopulateSelectListsAsync(booking.VenueId, booking.EventId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Booking booking)
        {
            await ValidateBookingAsync(booking, booking.BookingId);

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(booking.VenueId, booking.EventId);
                return View(booking);
            }

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Booking updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Booking deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateBookingAsync(Booking booking, int? ignoreBookingId = null)
        {
            if (booking.EndDate <= booking.StartDate)
            {
                ModelState.AddModelError(nameof(Booking.EndDate), "End date must be after the start date.");
            }

            var selectedEvent = await _context.Events.FirstOrDefaultAsync(e => e.EventId == booking.EventId);
            if (selectedEvent != null && selectedEvent.VenueId != booking.VenueId)
            {
                ModelState.AddModelError("", "The selected event is linked to a different venue. Please choose the venue that belongs to that event.");
            }

            var overlapsExistingBooking = await _context.Bookings.AnyAsync(b =>
                (!ignoreBookingId.HasValue || b.BookingId != ignoreBookingId.Value) &&
                b.VenueId == booking.VenueId &&
                b.Status != "Cancelled" &&
                booking.StartDate < b.EndDate &&
                booking.EndDate > b.StartDate);

            if (overlapsExistingBooking)
            {
                ModelState.AddModelError("", "This venue is already booked during the selected date and time.");
            }
        }

        private async Task PopulateSelectListsAsync(int? venueId = null, int? eventId = null)
        {
            var events = await _context.Events
                .Include(e => e.Venue)
                .OrderBy(e => e.EventName)
                .Select(e => new
                {
                    e.EventId,
                    DisplayName = e.Venue == null
                        ? e.EventName
                        : e.EventName + " - " + e.Venue.VenueName
                })
                .ToListAsync();

            ViewBag.Events = new SelectList(events, "EventId", "DisplayName", eventId);
            ViewBag.Venues = new SelectList(await _context.Venues.OrderBy(v => v.VenueName).ToListAsync(), "VenueId", "VenueName", venueId);
        }
    }
}
