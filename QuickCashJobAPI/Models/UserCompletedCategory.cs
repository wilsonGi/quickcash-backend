using QuickCashJobAPI.Models.DTO;

namespace QuickCashJobAPI.Models
{
    public class UserCompletedCategory
    {
        public int Id { get; set; }

        public string UserId { get; set; }  // Foreign key to ApplicationUser
        public int CategoryId { get; set; } // Foreign key to Category

        public virtual ApplicationUser User { get; set; }
        public virtual Category Category { get; set; }
    }
}
