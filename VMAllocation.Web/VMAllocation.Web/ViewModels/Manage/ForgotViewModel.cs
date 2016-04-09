using System.ComponentModel.DataAnnotations;

namespace VMAllocation.Web.ViewModels.Manage
{
    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}