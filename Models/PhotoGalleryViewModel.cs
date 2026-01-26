namespace HelloWorld.NetCore.Models
{
    public class PhotoGalleryViewModel
    {
        public string SetName { get; set; } = string.Empty;
        public List<string> PhotoFiles { get; set; } = new List<string>();
    }

    public class PhotoGalleryIndexViewModel
    {
        public List<PhotoSet> PhotoSets { get; set; } = new List<PhotoSet>();
    }

    public class PhotoSet
    {
        public string Name { get; set; } = string.Empty;
        public int PhotoCount { get; set; }
    }
}
