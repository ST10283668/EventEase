using Microsoft.AspNetCore.Mvc;
using EventEase.Data;
using EventEase.Models;

namespace EventEase.Controllers
{
    public class VenuesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VenuesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var venues = _context.Venues.ToList();
            return View(venues);
        }
    }
}