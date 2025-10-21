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
                    options.UseMySql(
                        "Server=localhost;Database=YourDatabaseName;User=root;Password=your_password;",
                        ServerVersion.Create(8, 0, 0, ServerType.MySql),
                        optionsBuilder => optionsBuilder
                            .DisableLineBreakToCharSubstition()
                            .SchemaBehavior(MySqlSchemaBehavior.Ignore)
                        );
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
            modelBuilder.Entity<Blog>()
                .ToTable("Blogs", schema: "Blogging");

            modelBuilder.Entity<AuditEntry>().HasNoKey();

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

    public class AuditEntry
    {
        public string? Name { get; set; }
    }

    // Second DbContext for testing --context flag
    public class ShopContextFactory : IDesignTimeDbContextFactory<ShopContext>
    {
        public ShopContext CreateDbContext(string[] args)
        {
            var provider = args.FirstOrDefault();
            return new ShopContext(provider!);
        }
    }

    public class ShopContext : DbContext
    {
        public DbSet<Product>? Products { get; set; }
        public DbSet<Category>? Categories { get; set; }

        private readonly string _provider;
        public ShopContext(string provider = "SqlServer")
        {
            _provider = provider;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            switch (_provider.ToLower())
            {
                case "sqlserver":
                    options.UseSqlServer("Server=localhost;Database=ShopDb;User Id=your_username;Password=your_password;");
                    break;
                case "sqlite":
                    options.UseSqlite("Data Source=shop.db;");
                    break;
                case "mysql":
                    options.UseMySql(
                        "Server=localhost;Database=ShopDb;User=root;Password=your_password;",
                        ServerVersion.Create(8, 0, 0, ServerType.MySql),
                        optionsBuilder => optionsBuilder
                            .DisableLineBreakToCharSubstition()
                            .SchemaBehavior(MySqlSchemaBehavior.Ignore)
                        );
                    break;
                case "mariadb":
                    options.UseMySql("Server=localhost;Database=ShopDb;User=root;Password=your_password;", ServerVersion.Create(8, 7, 0, ServerType.MariaDb));
                    break;
                case "postgres":
                    options.UseNpgsql("Host=localhost;Database=ShopDb;Username=your_username;Password=your_password;");
                    break;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>()
                .ToTable("Products", schema: "Shop");

            modelBuilder.Entity<Category>()
                .ToTable("Categories", schema: "Shop");

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);

            modelBuilder.Entity<Product>()
                .Property(p => p.Name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .HasMaxLength(50)
                .IsRequired();
        }
    }

    public class Product
    {
        [Key]
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }

    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Product>? Products { get; set; }
    }
}