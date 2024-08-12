using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace DemoNamespace
{
  public class Program
  {
    public static void Main(string[] args)
    {
    }
  }

  public class BloggingContextFactory : IDesignTimeDbContextFactory<BloggingContext>
  {
    public BloggingContext CreateDbContext(string[] args)
    {
      var provider = args.FirstOrDefault();

      return new BloggingContext(provider!);
    }
  }
  public class BloggingContext : DbContext
  {
    public DbSet<Blog>? Blogs { get; set; }

    private readonly string _provider;
    public BloggingContext(string provider = "SqlServer")
    {
      _provider = provider;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
      switch (_provider.ToLower())
      {
        case "sqlserver":
          options.UseSqlServer("Server=localhost;Database=YourDatabaseName;User Id=your_username;Password=your_password;");
          break;
        case "sqlite":
          options.UseSqlite("Data Source=localdatabase.db;");
          break;
        case "mysql":
          options.UseMySql("Server=localhost;Database=YourDatabaseName;User=root;Password=your_password;", ServerVersion.Create(8, 0, 0, ServerType.MySql));
          break;
        case "mariadb":
          options.UseMySql("Server=localhost;Database=YourDatabaseName;User=root;Password=your_password;", ServerVersion.Create(8, 7, 0, ServerType.MariaDb));
          break;
        case "postgres":
          options.UseNpgsql("Host=localhost;Database=YourDatabaseName;Username=your_username;Password=your_password;");
          break;
      }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Post>()
          .HasOne(p => p.Blog)
          .WithMany(b => b.Posts)
          .HasForeignKey(p => p.BlogUrl)
          .HasPrincipalKey(b => b.Url);

      modelBuilder.Entity<Blog>()
          .HasMany(b => b.Posts)
          .WithOne(p => p.Blog)
          .HasForeignKey(p => p.BlogUrl);

      modelBuilder.Entity<Blog>()
          .Property(b => b.Author)
          .HasDefaultValue("Anonymous")
          .HasMaxLength(200);
    }
  }

  public class Blog
  {
    [Key]
    public int BlogId { get; set; }

    [Column(TypeName = "varchar(200)")]
    public string? Url { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal Rating { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<Post>? Posts { get; set; }
  }

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