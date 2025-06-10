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
}