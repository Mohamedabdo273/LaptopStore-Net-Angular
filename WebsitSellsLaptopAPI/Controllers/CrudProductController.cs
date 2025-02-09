using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using WebsitSellsLaptop.Models;
using WebsitSellsLaptop.Repository;
using WebsitSellsLaptop.Repository.IRepository;
using WebsitSellsLaptop.Utility;

namespace WebsitSellsLaptop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = SD.adminRole)]
    public class CrudProductController : ControllerBase
    {
        private readonly IProduct _product;

        public CrudProductController(IProduct product)
        {
            _product = product;
        }

        // GET with Pagination
        [HttpGet("AllProducts")]
        public IActionResult AllProducts(int pageNumber = 1)
        {
            int pageSize = 10;
            var products = _product.Get();
            var paginatedProducts = products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var response = new
            {
                TotalProducts = products.Count(),
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling((double)products.Count() / pageSize),
                Products = paginatedProducts
            };

            return Ok(response);
        }
        
        // CREATE
        [HttpPost("Create")]
       
        public IActionResult Create([FromForm]Product product,IFormFile? productImgs)
        {
            if (ModelState.IsValid)
            {
                if (productImgs != null && productImgs.Length > 0)
                {
                    // Get the directory of the Infrastructure project
                    var infrastructurePath = Path.Combine(Directory.GetCurrentDirectory(), "Images");

                    // Ensure the Images directory exists
                    if (!Directory.Exists(infrastructurePath))
                    {
                        Directory.CreateDirectory(infrastructurePath);
                    }

                    // Generate a unique file name for the image
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(productImgs.FileName);
                    var filePath = Path.Combine(infrastructurePath, fileName);

                    // Save the file to the folder
                    using (var stream = System.IO.File.Create(filePath))
                    {
                        productImgs.CopyTo(stream);
                    }

                    // Store the file name in the product object (relative to the folder for later retrieval)
                    product.ImgUrl = fileName;
                }
                else
                {
                    product.ImgUrl = string.Empty; // No image uploaded
                }

                _product.Create(product);
                _product.Commit();

                // Optional: Add a success cookie
                CookieOptions cookieOptions = new()
                {
                    Expires = DateTime.Now.AddMinutes(1)
                };

                Response.Cookies.Append("success", "Product added successfully", cookieOptions);

                return Ok(product);
            }

            return BadRequest(ModelState);
        }
        // GET Product by ID
        [HttpGet("GetById/{id}")]
        public IActionResult GetById(int id)
        {
            var product = _product.GetOne([e=>e.Category],expression: x => x.Id == id);

            if (product == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            return Ok(product);
        }

        // EDIT
        [HttpPut("Edit/{id}")]
        public IActionResult Edit([FromForm] Product product, IFormFile photoUrl)
        {
            var oldProduct = _product.GetOne(expression: e => e.Id == product.Id, tracked: false);
            ModelState.Remove("photoUrl");
            if (oldProduct == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            if (ModelState.IsValid)
            {
                // Get the directory of the Infrastructure project
                var infrastructurePath = Path.Combine(Directory.GetCurrentDirectory(),"Images");

                // Handle image update
                if (photoUrl != null && photoUrl.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photoUrl.FileName);
                    var filePath = Path.Combine(infrastructurePath, fileName);
                    var oldFilePath = Path.Combine(infrastructurePath, oldProduct.ImgUrl);

                    // Save the new file
                    using (var stream = System.IO.File.Create(filePath))
                    {
                        photoUrl.CopyTo(stream);
                    }

                    // Delete the old file if it exists
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }

                    product.ImgUrl = fileName;
                }

                _product.Edit(product);
                _product.Commit();

                return Ok();
            }

            return BadRequest(ModelState);
        }

        // DELETE
        [HttpDelete("Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var productToDelete = _product.GetOne(expression: x => x.Id == id);
            if (productToDelete == null)
            {
                return NotFound(new { Message = "Product not found" });
            }

            // Get the directory of the Infrastructure project
            var infrastructurePath = Path.Combine(Directory.GetCurrentDirectory(),"Images");

            // Delete the associated image file
            var oldFilePath = Path.Combine(infrastructurePath, productToDelete.ImgUrl);
            if (System.IO.File.Exists(oldFilePath))
            {
                System.IO.File.Delete(oldFilePath);
            }

            _product.Delete(productToDelete);
            _product.Commit();
            return Ok(new { Message = "Product deleted successfully" });
        }
    }
}
