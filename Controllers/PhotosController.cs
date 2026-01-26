using Microsoft.AspNetCore.Mvc;
using HelloWorld.NetCore.Models;

namespace HelloWorld.NetCore.Controllers
{
    public class PhotosController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public PhotosController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // Main gallery index - dynamically discovers all photo sets
        public IActionResult Index()
        {
            var photosPath = Path.Combine(_environment.ContentRootPath, "Data", "Photos");
            var photoSets = new List<PhotoSet>();

            if (Directory.Exists(photosPath))
            {
                var directories = Directory.GetDirectories(photosPath);
                
                foreach (var directory in directories)
                {
                    var dirName = Path.GetFileName(directory);
                    var photoCount = Directory.GetFiles(directory)
                        .Count(f => IsImageFile(Path.GetFileName(f)));

                    photoSets.Add(new PhotoSet
                    {
                        Name = dirName,
                        PhotoCount = photoCount
                    });
                }
            }

            var model = new PhotoGalleryIndexViewModel
            {
                PhotoSets = photoSets
            };

            return View(model);
        }

        // View photos from any set dynamically
        public IActionResult ViewSet(string setName)
        {
            if (string.IsNullOrEmpty(setName))
            {
                return RedirectToAction("Index");
            }

            var photosPath = Path.Combine(_environment.ContentRootPath, "Data", "Photos", setName);
            
            if (!Directory.Exists(photosPath))
            {
                return NotFound($"Photo set '{setName}' not found.");
            }

            var photoFiles = Directory.GetFiles(photosPath)
                .Select(f => Path.GetFileName(f))
                .Where(f => IsImageFile(f))
                .ToList();

            var model = new PhotoGalleryViewModel
            {
                SetName = setName,
                PhotoFiles = photoFiles
            };

            return View(model);
        }

        private bool IsImageFile(string filename)
        {
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            return extensions.Any(ext => filename.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
    }
}
