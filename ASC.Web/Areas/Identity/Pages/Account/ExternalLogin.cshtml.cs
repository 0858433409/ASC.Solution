using ASC.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace ASC.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                

            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                /*ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });*/
                TempData["ErrorMessage"] = "Account has been locked.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });

            }
            TempData["LoginProvider"] = info.LoginProvider;
            TempData["ProviderKey"] = info.ProviderKey;
            TempData["Email"] = info.Principal.FindFirstValue(ClaimTypes.Email);
            // TH1: User đã từng đăng nhập bằng Google trước đó
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                var claims = await _userManager.GetClaimsAsync(user);
                var isActive = Boolean.Parse(claims.SingleOrDefault(p => p.Type == "IsActive")?.Value ?? "false");

                if (!isActive)
                {
                    ModelState.AddModelError(string.Empty, "Account has been locked.");
                    return Page();
                }

                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider);
                return RedirectToAction("Dashboard", "Dashboard", new { Area = "ServiceRequestt" });
            }

            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            // TH2: User chưa từng đăng nhập bằng Google, nhưng email đã có trong hệ thống
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    var claims = await _userManager.GetClaimsAsync(existingUser);
                    var isActive = Boolean.Parse(claims.SingleOrDefault(p => p.Type == "IsActive")?.Value ?? "false");

                    if (!isActive)
                    {
                        ModelState.AddModelError(string.Empty, "Account has been locked.");
                        return Page();
                    }

                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                    return RedirectToAction("Dashboard", "Dashboard", new { Area = "ServiceRequestt" });
                }

                // Nếu liên kết tài khoản thất bại
                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return RedirectToPage("./Login");
            }

            // TH3: Người dùng hoàn toàn mới → chuyển sang trang đăng ký để tạo tài khoản
            ReturnUrl = returnUrl;
            ProviderDisplayName = info.ProviderDisplayName;
            Input = new InputModel { Email = email }; // hỗ trợ bind email sẵn
            var providerKey = TempData["ProviderKey"]?.ToString();
            var loginProvider = TempData["LoginProvider"]?.ToString();


            return Page(); // Trả về trang Register.cshtml để nhập email và đăng ký
        }


        /*public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created account using {Name} provider.", info.LoginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            null,
                            new { area = "Identity", userId, code },
                            Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("/RegisterConfirmation", new { Email = Input.Email });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = info.ProviderDisplayName;
            ReturnUrl = returnUrl;
            return Page();
        }*/
        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Lấy lại thông tin từ TempData (đã lưu ở OnGetCallbackAsync)
            var loginProvider = TempData["LoginProvider"]?.ToString();
            var providerKey = TempData["ProviderKey"]?.ToString();
            var email = TempData["Email"]?.ToString();

            if (loginProvider == null || providerKey == null)
            {
                TempData["ErrorMessage"] = "Login information has expired. Please try logging in again.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = new ExternalLoginInfo(new ClaimsPrincipal(), loginProvider, providerKey, loginProvider);

            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    var loginInfo = new UserLoginInfo(loginProvider, providerKey, loginProvider);
                    var addLoginResult = await _userManager.AddLoginAsync(user, loginInfo);

                    if (addLoginResult.Succeeded)
                    {
                        _logger.LogInformation("User created an account using {Name} provider.", loginProvider);

                        var userId = await _userManager.GetUserIdAsync(user);
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                        var callbackUrl = Url.Page(
                            "/Account/ConfirmEmail",
                            null,
                            new { area = "Identity", userId, code },
                            Request.Scheme);

                        await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        if (_userManager.Options.SignIn.RequireConfirmedAccount)
                        {
                            return RedirectToPage("/RegisterConfirmation", new { Email = Input.Email });
                        }

                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }

                    // Thêm liên kết thất bại
                    foreach (var error in addLoginResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

                // Tạo user thất bại
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ProviderDisplayName = loginProvider;
            ReturnUrl = returnUrl;
            return Page();
        }

    }
}
