using ASC.Model.BaseType;
using ASC.Solution.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace ASC.Web.Data
{
    public class IdentitySeed : IIdentitySeed
    {
        private readonly ILogger<IdentitySeed> _logger;

        public IdentitySeed(ILogger<IdentitySeed> logger)
        {
            _logger = logger;
        }

        public async Task Seed(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
            IOptions<ApplicationSettings> options)
        {
            var roles = options.Value.Roles.Split(',');

            // Create roles if they don't exist
            foreach (var role in roles)
            {
                try
                {
                    if (!await roleManager.RoleExistsAsync(role)) // ✅ Fix deadlock
                    {
                        IdentityRole storageRole = new IdentityRole { Name = role };
                        IdentityResult roleResult = await roleManager.CreateAsync(storageRole);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating role {role}: {ex.Message}");
                }
            }

            // Tạo Admin User
            var admin = await userManager.FindByEmailAsync(options.Value.AdminEmail);
            if (admin == null)
            {
                IdentityUser user = new IdentityUser
                {
                    UserName = options.Value.AdminName,
                    Email = options.Value.AdminEmail,
                    EmailConfirmed = true
                };

                IdentityResult result = await userManager.CreateAsync(user, options.Value.AdminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", options.Value.AdminEmail));
                    await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("IsActive", "True"));

                    await userManager.AddToRoleAsync(user, Roles.Admin.ToString());
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Error creating Admin user: {error.Description}");
                    }
                }
            }

            // Tạo Engineer User (Fix lỗi trùng email)
            var engineer = await userManager.FindByEmailAsync(options.Value.EngineerEmail);
            if (engineer == null)
            {
                IdentityUser user = new IdentityUser
                {
                    UserName = options.Value.EngineerName, // ✅ Fix sai UserName
                    Email = options.Value.EngineerEmail,   // ✅ Fix sai Email
                    EmailConfirmed = true,
                    LockoutEnabled = false
                };

                IdentityResult result = await userManager.CreateAsync(user, options.Value.EngineerPassword);

                if (result.Succeeded)
                {
                    await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", options.Value.EngineerEmail));
                    await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("IsActive", "True"));

                    await userManager.AddToRoleAsync(user, Roles.Engineer.ToString());
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError($"Error creating Engineer user: {error.Description}");
                    }
                }
            }
        }
    }
}
