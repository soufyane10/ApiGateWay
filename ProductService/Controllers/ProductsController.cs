using Microsoft.AspNetCore.Mvc;
using ProductService.Models;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private static List<Product> _products = new List<Product>()
        {
            new Product { Id = 1, Name = "Laptop", Description = "High-performance laptop", Price = 1200.00m },
            new Product { Id = 2, Name = "Mouse", Description = "Wireless optical mouse", Price = 25.00m },
            new Product { Id = 3, Name = "Keyboard", Description = "Mechanical gaming keyboard", Price = 99.99m }
        };

        [HttpGet]
        public IActionResult GetProducts()
        {
            return Ok(_products);
        }

        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPost]
        public IActionResult CreateProduct([FromBody] Product newProduct)
        {
            if (newProduct == null || string.IsNullOrEmpty(newProduct.Name) || newProduct.Price <= 0)
            {
                return BadRequest("Invalid product data.");
            }

            newProduct.Id = _products.Max(p => p.Id) + 1; // Simple ID generation
            _products.Add(newProduct);
            return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            if (updatedProduct == null || updatedProduct.Id != id || string.IsNullOrEmpty(updatedProduct.Name) || updatedProduct.Price <= 0)
            {
                return BadRequest("Invalid product data.");
            }

            var existingProduct = _products.FirstOrDefault(p => p.Id == id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var productToRemove = _products.FirstOrDefault(p => p.Id == id);
            if (productToRemove == null)
            {
                return NotFound();
            }

            _products.Remove(productToRemove);
            return NoContent();
        }
    }
}