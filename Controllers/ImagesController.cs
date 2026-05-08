using EventEase.Services;
using Microsoft.AspNetCore.Mvc;

namespace EventEase.Controllers
{
    public class ImagesController : Controller
    {
        private readonly BlobStorageService _blobStorageService;

        public ImagesController(BlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public async Task<IActionResult> Show(string name)
        {
            var image = await _blobStorageService.DownloadAsync(name);

            if (image == null)
            {
                return NotFound();
            }

            return File(image.Content.ToStream(), image.Details.ContentType ?? "image/jpeg");
        }
    }
}
