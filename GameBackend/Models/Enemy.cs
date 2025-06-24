using System;

namespace ShakesGodmist.Models
{
    public class Enemy
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Intelligence { get; set; }
        public int Constitution { get; set; }
        public int Luck { get; set; }
        public int MinDamage { get; set; }
        public int MaxDamage { get; set; }
        public int Armor { get; set; }
        public double CritChance { get; set; } // 0-1
        public int HitPoints { get; set; }
    }
} 