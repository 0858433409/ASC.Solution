using ASC.Model.BaseType;
using ASC.Web.Areas.Accounts.Models;
using ASC.Web.Services;
using ASC.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ASC.Web.Areas.Accounts.Controllers
{
    [Authorize]
    [Area("Accounts")]
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager, IEmailSender emailSender, SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpGet]
        [Authorize(Roles = "Admin")]
         [HttpGet]
    public async Task<IActionResult> ServiceEngineers()
    {
        var engineers = await _userManager.GetUsersInRoleAsync(Roles.Engineer.ToString());

        var model = new ServiceEngineerViewModel
        {
            ServiceEngineers = engineers?.ToList(),
            Registration = new ServiceEngineerRegistrationViewModel()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ServiceEngineers(ServiceEngineerViewModel model)
    {
        model.ServiceEngineers = (await _userManager.GetUsersInRoleAsync(Roles.Engineer.ToString())).ToList();

        var reg = model.Registration;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(reg.Email);

        if (reg.IsEdit)
        {
            // Chỉnh sửa
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy người dùng để chỉnh sửa.");
                return View(model);
            }

            user.UserName = reg.UserName;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                updateResult.Errors.ToList().ForEach(err => ModelState.AddModelError("", err.Description));
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(reg.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, reg.Password);

                if (!passResult.Succeeded)
                {
                    passResult.Errors.ToList().ForEach(err => ModelState.AddModelError("", err.Description));
                    return View(model);
                }
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var activeClaim = claims.FirstOrDefault(c => c.Type == "IsActive");
            if (activeClaim != null)
                await _userManager.RemoveClaimAsync(user, activeClaim);

            await _userManager.AddClaimAsync(user, new Claim("IsActive", reg.IsActive.ToString()));
        }
        else
        {
            // Tạo mới
            if (user != null)
            {
                ModelState.AddModelError("", "Người dùng đã tồn tại.");
                return View(model);
            }

            user = new IdentityUser
            {
                UserName = reg.UserName,
                Email = reg.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, reg.Password);
            if (!result.Succeeded)
            {
                result.Errors.ToList().ForEach(err => ModelState.AddModelError("", err.Description));
                return View(model);
            }

            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Email, reg.Email));
            await _userManager.AddClaimAsync(user, new Claim("IsActive", reg.IsActive.ToString()));
            await _userManager.AddToRoleAsync(user, Roles.Engineer.ToString());
        }

        // Gửi email
        var subject = reg.IsActive ? "Tài khoản kỹ sư đã được tạo/cập nhật" : "Tài khoản đã bị vô hiệu hóa";
        var body = reg.IsActive
            ? $"Tài khoản của bạn đã được tạo hoặc cập nhật.\nEmail: {reg.Email}\nMật khẩu: {reg.Password}"
            : "Tài khoản của bạn đã bị vô hiệu hóa.";

        await _emailSender.SendEmailAsync(reg.Email, subject, body);

        return RedirectToAction(nameof(ServiceEngineers));
    }
        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            var users = await _userManager.GetUsersInRoleAsync("Customer");

            var model = new CustomerViewModel
            {
                Customers = users.ToList(),
                Registration = new CustomerRegistrationViewModel()
            };

            return View(model);
        }

        // POST: Accounts/Customers/Customers
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Customers(CustomerViewModel customer)
        {
            // Reload danh sách để hiển thị lại nếu có lỗi
            customer.Customers = (await _userManager.GetUsersInRoleAsync("Customer")).ToList();

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            var reg = customer.Registration;

            if (reg.IsEdit)
            {
                var user = await _userManager.FindByEmailAsync(reg.Email);
                if (user == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy người dùng.");
                    return View(customer);
                }

                // Cập nhật claim IsActive
                var claims = await _userManager.GetClaimsAsync(user);
                var isActiveClaim = claims.FirstOrDefault(c => c.Type == "IsActive");
                if (isActiveClaim != null)
                {
                    await _userManager.RemoveClaimAsync(user, isActiveClaim);
                }
                await _userManager.AddClaimAsync(user, new Claim("IsActive", reg.IsActive.ToString()));

                // Gửi email thông báo
                var subject = reg.IsActive ? "Tài khoản được kích hoạt" : "Tài khoản bị vô hiệu hóa";
                var body = reg.IsActive
                    ? $"Tài khoản của bạn đã được kích hoạt.\nEmail: {reg.Email}"
                    : "Tài khoản của bạn đã bị vô hiệu hóa.";

                await _emailSender.SendEmailAsync(reg.Email, subject, body);
            }

            return RedirectToAction(nameof(Customers));
        }
        [HttpGet]
        public IActionResult Profile()
        {
            var user = HttpContext.User.GetCurrentUserDetails();
            return View(new ProfileModel() { UserName = user.Name });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileModel profile)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Update UserName
            var user = await _userManager.FindByEmailAsync(HttpContext.User.GetCurrentUserDetails().Email);
            user.UserName = profile.UserName;
            IdentityResult result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                result.Errors.ToList().ForEach(p => ModelState.AddModelError("", p.Description));
                return View();
            }
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Dashboard", "Dashboard", new { area = "ServiceRequests" });
        }

    }
}