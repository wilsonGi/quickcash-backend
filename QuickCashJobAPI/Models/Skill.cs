using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class Skill
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        // Navigation
        public ICollection<UserSkill> UserSkills { get; set; }
    }
}
