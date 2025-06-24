using System.ComponentModel.DataAnnotations;

namespace ShakesGodmist.Models
{
    public class Player
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Gold { get; set; } = 10;
        public int Strength { get; set; } = 10;
        public int Dexterity { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Constitution { get; set; } = 10;
        public int Luck { get; set; } = 10;
        public int StrengthUpgrades { get; set; } = 0;
        public int DexterityUpgrades { get; set; } = 0;
        public int IntelligenceUpgrades { get; set; } = 0;
        public int ConstitutionUpgrades { get; set; } = 0;
        public int LuckUpgrades { get; set; } = 0;
        
        public PlayerClass Class { get; set; } = PlayerClass.Warrior;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActive { get; set; } = DateTime.UtcNow;
        
        // Foreign key for user ownership
        public int UserId { get; set; }
        public virtual User User { get; set; }
        
        // Navigation properties
        public virtual ICollection<Item> Items { get; set; } = new List<Item>();
        public virtual ICollection<Quest> ActiveQuests { get; set; } = new List<Quest>();
    }

    public enum PlayerClass
    {
        Warrior = 1,
        Mage = 2,
        Scout = 3
    }
}