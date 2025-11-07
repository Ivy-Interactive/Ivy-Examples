namespace PuppeteerSharpExample
{
    public static class MemoryFileStore
    {
        private static readonly Dictionary<string, (byte[] Data, string ContentType, string FileName)> _files = new();

        public static string Add(byte[] data, string contentType, string fileName)
        {
            var id = Guid.NewGuid().ToString("N");
            _files[id] = (data, contentType, fileName);
            return id;
        }

        public static (byte[] Data, string ContentType, string FileName)? Get(string id)
        {
            if (_files.TryGetValue(id, out var file))
                return new (file.Data, file.ContentType, file.FileName);

            _files.Remove(id);
            return null;
        }

        public static void Remove(string id) => _files.Remove(id);
    }
}
