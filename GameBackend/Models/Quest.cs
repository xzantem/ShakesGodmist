using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShakesGodmist.Models
{
    public class Quest
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        public double Duration { get; set; } // in minutes (supports decimals)
        public int ExperienceReward { get; set; }
        public int GoldReward { get; set; }
        public int RequiredLevel { get; set; } = 1;
        public QuestType Type { get; set; }
        
        // For active quests
        public int? PlayerId { get; set; }
        public virtual Player Player { get; set; }
        public DateTime? StartedAt { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool? GrantsItem { get; set; } = false;

        // Persisted as JSON in the database
        public string EnemyJson { get; set; }

        [NotMapped]
        public Enemy Enemy
        {
            get => string.IsNullOrEmpty(EnemyJson) ? null : JsonSerializer.Deserialize<Enemy>(EnemyJson);
            set => EnemyJson = JsonSerializer.Serialize(value);
        }

        public string GrantedItemJson { get; set; }

        [NotMapped]
        public Item GrantedItem
        {
            get => string.IsNullOrEmpty(GrantedItemJson) ? null : JsonSerializer.Deserialize<Item>(GrantedItemJson);
            set => GrantedItemJson = JsonSerializer.Serialize(value);
        }
    }

    public enum QuestType
    {
        Adventure = 1,
        Work = 2,
        Arena = 3
    }
}