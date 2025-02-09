using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using WebsitSellsLaptop.Models;
using WebsitSellsLaptop.Repository;
using WebsitSellsLaptop.Repository.IRepository;
using WebsitSellsLaptop.Utility;

namespace WebsitSellsLaptop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProduct product;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly ICard card;
        private readonly IOrder order;
        
        public ProductController(IProduct product, UserManager<ApplicationUser> userManager, ICard card, IOrder order, IContactUs contactUs)
        {
            this.product = product;
            this.userManager = userManager;
            this.card = card;
            this.order = order;
            
        }

        [HttpGet("Get")]
        public IActionResult Get(string? search = null, int page = 1, string? category = null, decimal? minPrice = null, decimal? maxPrice = null )
        {
            int pageSize = 5;
            try
            {
                if (page <= 0)
                {
                    return BadRequest(new { message = "Page number must be greater than 0." });
                }

                if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
                {
                    return BadRequest(new { message = "Minimum price cannot be greater than maximum price." });
                }

                pageSize = pageSize > 0 ? pageSize : 5;

                var products = product.Get([e => e.Category]);

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.Trim();
                    products = products.Where(e => e.Name.Contains(search));
                }

                if (!string.IsNullOrEmpty(category))
                {
                    if (int.TryParse(category, out int categoryId))
                    {
                        products = products.Where(e => e.Category.Id == categoryId);
                    }
                    else
                    {
                        products = products.Where(e => e.Category.Name == category);
                    }
                }


                if (minPrice.HasValue)
                {
                    products = products.Where(e => e.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    products = products.Where(e => e.Price <= maxPrice.Value);
                }

                int totalProducts = products.Count();
                int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

                if (page > totalPages) page = totalPages;

                var paginatedProducts = products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    Data = paginatedProducts,
                    TotalPages = totalPages,
                    CurrentPage = page
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the products.", error = ex.Message });
            }
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(int id)
        {
            try
            {
                var productDetail = product.GetOne([e => e.Category], expression: e => e.Id == id);
                if (productDetail == null)
                    return NotFound();

                return Ok(productDetail);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the product details.", error = ex.Message });
            }
        }

        [HttpPost("AddToCart")]
        [Authorize]
        public IActionResult AddToCart(int productId, int count)
        {
            try
            {
                if (count <= 0)
                {
                    return BadRequest(new { message = "Count must be greater than 0." });
                }

                var appUser = userManager.GetUserId(User);
                if (appUser == null)
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }

                var productItem = product.Get(expression: e => e.Id == productId).FirstOrDefault();
                if (productItem == null)
                {
                    return NotFound(new { message = "Product not found." });
                }

                if (count > productItem.count)
                {
                    return BadRequest(new { message = "Requested quantity exceeds available stock." });
                }

                var existingCart = card.GetOne(expression: e => e.ProductId == productId && e.UserId == appUser);
                int totalCount = existingCart != null ? existingCart.count + count : count;

                if (totalCount > productItem.count)
                {
                    return BadRequest(new { message = "Total cart quantity exceeds available stock." });
                }

                if (existingCart == null)
                {
                    card.Create(new Card
                    {
                        count = count,
                        ProductId = productId,
                        UserId = appUser
                    });
                }
                else
                {
                    existingCart.count += count;
                }

                card.Commit();
                return Ok(new { message = "Product added to cart." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while adding the product to the cart.", error = ex.Message });
            }
        }

        [HttpGet("Cart/GetItems")]
        [Authorize]
        public IActionResult GetCartItems()
        {
            try
            {
                var appUserId = userManager.GetUserId(User);
                if (appUserId == null)
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }
                var cartItems = card.Get([e => e.product], expression: e => e.UserId == appUserId).ToList();
                if (!cartItems.Any())
                {
                    return NotFound(new { message = "No items in the cart." });
                }
                return Ok(cartItems);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the cart items.", error = ex.Message });
            }
        }
        [HttpPut("Increment/{id}")]
        [Authorize]
        public IActionResult Increment(int id)
        {
            try
            {
                var appUser = userManager.GetUserId(User);
                var productCart = card.GetOne(expression: e => e.ProductId == id && e.UserId == appUser);
                var productItem = product.GetOne(expression: e => e.Id == id);

                if (productCart == null || productItem == null)
                    return NotFound(new { message = "Cart item or product not found." });

                if (productCart.count + 1 > productItem.count)
                    return BadRequest(new { message = "Cannot exceed available stock." });

                productCart.count++;
                card.Commit();
                return Ok(new { message = "Cart item incremented." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while incrementing the cart item.", error = ex.Message });
            }
        }


        [HttpPut("Decrement/{id}")]
        [Authorize]
        public IActionResult Decrement(int id)
        {
            try
            {
                var appUser = userManager.GetUserId(User);
                var productCart = card.GetOne(expression: e => e.ProductId == id && e.UserId == appUser);

                if (productCart == null)
                    return NotFound(new { message = "Cart item not found." });

                productCart.count--;
                if (productCart.count == 0)
                {
                    card.Delete(productCart);
                }
                card.Commit();
                return Ok(new { message = "Cart item decremented." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while decrementing the cart item.", error = ex.Message });
            }
        }

        [HttpDelete("Delete/{id}")]
        [Authorize]
        public IActionResult Delete(int id)
        {
            try
            {
                var appUser = userManager.GetUserId(User);
                var productCart = card.GetOne(expression: e => e.ProductId == id && e.UserId == appUser);

                if (productCart == null)
                    return NotFound(new { message = "Cart item not found." });

                card.Delete(productCart);
                card.Commit();
                return Ok(new { message = "Cart item deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while deleting the cart item.", error = ex.Message });
            }
        }
        [HttpPost("Pay")]
        [Authorize]
        public IActionResult Pay()
        {
            var appUser = userManager.GetUserId(User);
            if (appUser == null)
                return Unauthorized(new { message = "User is not authenticated." });

            var cartItems = card.Get([e => e.product], expression: e => e.UserId == appUser).ToList();
            if (!cartItems.Any())
                return BadRequest(new { message = "No items in the cart to pay for." });

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = $"http://localhost:4200/success", // Redirect to Angular success page
                CancelUrl = $"http://localhost:4200/cart", // Redirect to cart if canceled
            };

            foreach (var item in cartItems)
            {
                if (item.product.Price < 0)
                    return BadRequest(new { message = "Invalid product price." });

                options.LineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "egp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.product.Name,
                        },
                        UnitAmount = (long)(item.product.Price * 100),
                    },
                    Quantity = item.count,
                });
            }

            var service = new SessionService();
            var session = service.Create(options);
            return Ok(new { url = session.Url });
        }

        [HttpGet("Success")]
        [Authorize]
        public IActionResult Success()
        {
            var appUser = userManager.GetUserId(User);
            var appUserName = userManager.GetUserName(User);

            var cartItems = card.Get([e => e.product], expression: e => e.UserId == appUser).ToList();
            foreach (var item in cartItems)
            {
                if (item.product != null)
                {
                    // Create an order
                    order.Create(new Orders
                    {
                        UserName = appUserName,
                        ProductId = item.product.Id,
                        ProductName = item.product.Name,
                        count = item.count,
                        Date = DateTime.Now
                    });

                    // Update product stock
                    var productItem = product.GetOne(expression: e => e.Id == item.product.Id);
                    if (productItem != null)
                    {
                        productItem.count -= item.count;
                        product.Commit();
                    }

                    card.Delete(item);
                }
            }

            order.Commit();
            card.Commit();
            return Ok(new { message = "Payment successful." });
        }
    


[Authorize(Roles=SD.adminRole)]
        [HttpGet("AdminOrders")]
        public IActionResult Orders()
        {
            var orders = order.Get();
            return Ok(orders);
        }

        [HttpGet("Cancel")]
        [Authorize]
        public IActionResult Cancel()
        {
            return Ok(new { message = "Payment canceled." });
        }
        [Authorize]
        [HttpGet("UserOrders")]
        public IActionResult Order()
        {
            try
            {
                var appUserId = userManager.GetUserName(User);
                if (appUserId == null)
                {
                    return Unauthorized(new { message = "User is not authenticated." });
                }
                var userOrders = order.Get(expression: e => e.UserName == appUserId).ToList();
                if (!userOrders.Any())
                {
                    return NotFound(new { message = "No orders found for this user." });
                }
                return Ok(userOrders);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving the orders.", error = ex.Message });
            }
        }
    }
}
