using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ASC.Web.Areas.Accounts.Models
{
    public class ServiceEngineerViewModel
    {
       
        public List<IdentityUser>? ServiceEngineers { get; set; }

        public ServiceEngineerRegistrationViewModel Registration { get; set; }
    }

}