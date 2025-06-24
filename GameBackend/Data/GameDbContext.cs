using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using ShakesGodmist.Models;

namespace ShakesGodmist.Models
{
    public class GameContext : DbContext
    {
        public GameContext(DbContextOptions<GameContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Quest> Quests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Player configuration
            modelBuilder.Entity<Player>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<Player>()
                .HasOne(p => p.User)
                .WithMany(u => u.Characters)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Item configuration
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Player)
                .WithMany(p => p.Items)
                .HasForeignKey(i => i.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quest configuration
            modelBuilder.Entity<Quest>()
                .HasOne(q => q.Player)
                .WithMany(p => p.ActiveQuests)
                .HasForeignKey(q => q.PlayerId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Quest>()
                .Ignore(q => q.Enemy);

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Default enemy for seed quests
            var defaultEnemy = new Enemy {
                Name = "Seed Goblin",
                Level = 1,
                Strength = 8,
                Dexterity = 8,
                Intelligence = 8,
                Constitution = 8,
                Luck = 5,
                MinDamage = 2,
                MaxDamage = 4,
                Armor = 0,
                CritChance = 0.05,
                HitPoints = 45
            };
            var defaultEnemyJson = JsonSerializer.Serialize(defaultEnemy);

            // Default item for seed quests (if you want to seed one with an item)
            var defaultItem = new Item {
                Name = "Seed Sword",
                Description = "A basic sword for testing.",
                Type = ItemType.Weapon,
                Level = 1,
                MinDamage = 2,
                MaxDamage = 4,
                StrengthBonus = 1,
                DexterityBonus = 0,
                IntelligenceBonus = 0,
                ConstitutionBonus = 0,
                LuckBonus = 0,
                Armor = 0,
                Value = 10,
                IsEquipped = false
            };
            var defaultItemJson = JsonSerializer.Serialize(defaultItem);
            var noItemJson = "";

            // Seed base quests
            modelBuilder.Entity<Quest>().HasData(
                new Quest { Id = 1, Name = "Goblin Hunt", Description = "Hunt goblins in the forest", Duration = 30, ExperienceReward = 50, GoldReward = 25, RequiredLevel = 1, Type = QuestType.Adventure, EnemyJson = defaultEnemyJson, GrantedItemJson = noItemJson },
                new Quest { Id = 2, Name = "Tavern Work", Description = "Help at the local tavern", Duration = 60, ExperienceReward = 30, GoldReward = 50, RequiredLevel = 1, Type = QuestType.Work, EnemyJson = defaultEnemyJson, GrantedItemJson = noItemJson },
                new Quest { Id = 3, Name = "Arena Training", Description = "Train in the arena", Duration = 45, ExperienceReward = 75, GoldReward = 15, RequiredLevel = 2, Type = QuestType.Arena, EnemyJson = defaultEnemyJson, GrantedItemJson = defaultItemJson }
            );

            // Seed base items
            modelBuilder.Entity<Item>().HasData(
                new Item { Id = 1, Name = "Iron Sword", Description = "A sturdy iron sword", Type = ItemType.Weapon, Level = 1, StrengthBonus = 5, Value = 50 },
                new Item { Id = 2, Name = "Leather Armor", Description = "Basic leather protection", Type = ItemType.Armor, Level = 1, ConstitutionBonus = 3, Value = 30 },
                new Item { Id = 3, Name = "Magic Staff", Description = "A staff imbued with magic", Type = ItemType.Weapon, Level = 1, IntelligenceBonus = 5, Value = 60 }
            );
        }
    }
}