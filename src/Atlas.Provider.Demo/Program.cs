using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace DemoNamespace
{
    public class Program
    {
        public static void Main(string[] args)
        {

        }
    }

    public class BloggingContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlServer("foo");
        }
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int PostId { get; set; }
    }
}