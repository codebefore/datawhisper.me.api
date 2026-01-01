namespace DataWhisper.API.Models
{
    public class CachedQuery
    {
        public string SQL { get; set; }
        public string Prompt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
