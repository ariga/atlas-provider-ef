using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DemoNamespace
{
    public class Blog
    {
        [Key]
        public int BlogId { get; set; }

        [Column(TypeName = "varchar(200)")]
        public string? Url { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal Rating { get; set; }
        public string Title { get; set; } = string.Empty;
        [Comment("Content contains new lines \\n\\r and \n for example")]
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public List<Post>? Posts { get; set; }
    }
}
