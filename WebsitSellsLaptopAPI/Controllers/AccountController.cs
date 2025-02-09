using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebsitSellsLaptop.DTO;
using WebsitSellsLaptop.Models;
using WebsitSellsLaptop.Utility;

namespace WebsitSellsLaptop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        // Optional: In-memory token blacklist
        private static readonly List<string> TokenBlacklist = new();

        public AccountController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _configuration = configuration;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(ApplicationUserDto userDto)
        {
            // Ensure roles exist in the database
            if (!_roleManager.Roles.Any())
            {
                await _roleManager.CreateAsync(new IdentityRole(SD.adminRole));
                await _roleManager.CreateAsync(new IdentityRole(SD.CustomerRole));
            }

            if (ModelState.IsValid)
            {
                // Map DTO to ApplicationUser model
                var user = _mapper.Map<ApplicationUser>(userDto);

                // Create the user in the database
                var result = await _userManager.CreateAsync(user, userDto.Passwords);

                if (result.Succeeded)
                {
                    // Check if this is the first registered user
                    bool isFirstUser = !_userManager.Users.Any();

                    // Assign the appropriate role
                    if (isFirstUser)
                    {
                        // First user gets the Admin role
                        await _userManager.AddToRoleAsync(user, SD.adminRole);
                    }
                    else
                    {
                        // Subsequent users get the Customer role
                        if (!await _roleManager.RoleExistsAsync(SD.CustomerRole))
                        {
                            await _roleManager.CreateAsync(new IdentityRole(SD.CustomerRole));
                        }
                        await _userManager.AddToRoleAsync(user, SD.CustomerRole);
                    }

                    return Ok(new { Message = "Registration successful" });
                }

                // Add errors to the ModelState if registration fails
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }
            }

            return BadRequest(ModelState);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto userVm)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(userVm.UserName);
                if (user != null)
                {
                    if (await _userManager.CheckPasswordAsync(user, userVm.Password))
                    {
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, user.UserName),
                            new Claim(ClaimTypes.NameIdentifier, user.Id),
                            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                        };

                        var roles = await _userManager.GetRolesAsync(user);
                        foreach (var role in roles)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, role));
                        }

                        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
                        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                        var token = new JwtSecurityToken(
                            claims: claims,
                            issuer: _configuration["JWT:Issuer"],
                            audience: _configuration["JWT:Audience"],
                            expires: DateTime.Now.AddDays(14),
                            signingCredentials: creds
                        );

                        var response = new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(token),
                            expiration = token.ValidTo
                        };

                        return Ok(response);
                    }
                    else
                    {
                        return Unauthorized(new { Message = "Invalid password" });
                    }
                }
                else
                {
                    return Unauthorized(new { Message = "Invalid username" });
                }
            }

            return BadRequest(ModelState);
        }

        
    }
}
