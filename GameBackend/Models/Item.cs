using System.ComponentModel.DataAnnotations;

namespace ShakesGodmist.Models
{
    public class Item
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public int Level { get; set; }
        public int StrengthBonus { get; set; } = 0;
        public int DexterityBonus { get; set; } = 0;
        public int IntelligenceBonus { get; set; } = 0;
        public int ConstitutionBonus { get; set; } = 0;
        public int LuckBonus { get; set; } = 0;
        public int MinDamage { get; set; } = 0;
        public int MaxDamage { get; set; } = 0;
        public int Armor { get; set; } = 0;
        public int Value { get; set; }
        public bool IsEquipped { get; set; } = false;
        public bool IsShopItem { get; set; } = false;
        public int ShopSlot { get; set; } = -1;
        
        // Foreign Key
        public int? PlayerId { get; set; }
        public virtual Player Player { get; set; }
    }

    public enum ItemType
    {
        Weapon = 1,
        Helmet = 2,
        Armor = 3,
        Gloves = 4,
        Boots = 5,
        Shield = 6,
        Amulet = 7,
        Ring = 8
    }
}