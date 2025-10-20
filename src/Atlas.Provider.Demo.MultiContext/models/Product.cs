using System.ComponentModel.DataAnnotations;

namespace DemoNamespace
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }
}
