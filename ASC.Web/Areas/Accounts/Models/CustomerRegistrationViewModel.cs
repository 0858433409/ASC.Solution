using System.ComponentModel.DataAnnotations;

namespace ASC.Web.Areas.Accounts.Models
{
    public class CustomerRegistrationViewModel
    {
        [Required]
        public string Email { get; set; }

        public bool IsEdit { get; set; }

        public bool IsActive { get; set; }
    }
}