using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoNamespace
{
    [Table("Posts")]
    public class Post
    {
        [Key]
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? BlogUrl { get; set; }
        public Blog? Blog { get; set; }
    }
}
