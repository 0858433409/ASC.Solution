using ASC.Utilities;
using ASC.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace ASC.Web.Areas.Identity.Pages.Account
{
    public class InitiateResetPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public InitiateResetPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            // Lấy thông tin user
            var userEmail = HttpContext.User.GetCurrentUserDetails().Email;
            var user = await _userManager.FindByEmailAsync(userEmail);

            if (user == null)
            {
                return RedirectToPage("./ResetPasswordEmailConfirmation");
            }

            // Tạo và mã hóa token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // Tạo callback URL
            var callbackUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new
                {
                    email = userEmail,
                    code = encodedToken  // Sử dụng encoded token
                },
                protocol: Request.Scheme);

            // Gửi email
            await _emailSender.SendEmailAsync(
                userEmail,
                "Reset Password",
                $"Please reset your password by <a href='{callbackUrl}'>clicking here</a>.");

            return RedirectToPage("./ResetPasswordEmailConfirmation");
        }
    }
}