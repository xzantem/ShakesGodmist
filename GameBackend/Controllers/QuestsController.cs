using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShakesGodmist.Models;

namespace ShakesFidgetClone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuestsController : ControllerBase
    {
        private readonly GameContext _context;
        private readonly Random _random = new Random();

        public QuestsController(GameContext context)
        {
            _context = context;
        }
        public class StartQuestDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public double Duration { get; set; }
            public int ExperienceReward { get; set; }
            public int GoldReward { get; set; }
            public int RequiredLevel { get; set; }
            public QuestType Type { get; set; }
            
            // Item reward information
            public bool GrantsItem { get; set; }
            public string? GrantedItemJson { get; set; }
        }

        public class BattleStep
        {
            public string Attacker { get; set; } // "player", "enemy", or "init"
            public int Damage { get; set; }
            public bool Crit { get; set; }
            public int TargetHP { get; set; } // HP of the target after the attack
            public int? PlayerHP { get; set; } // Only set for the initial step
            public int? EnemyHP { get; set; } // Only set for the initial step
        }

        // GET: api/Quests
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Quest>>> GetAvailableQuests([FromQuery] int playerLevel = 1)
        {
            Console.WriteLine($"[DEBUG] GetAvailableQuests called - PlayerLevel: {playerLevel}");
            
            // Generate exactly 3 quests tailored to the player's level
            var quests = new List<Quest>();
            
            // Quest 1: Gold-focused (higher gold, lower XP) - shorter duration
            quests.Add(GenerateQuestForLevel(playerLevel, QuestFocus.Gold, isLonger: false));
            
            // Quest 2: XP-focused (higher XP, lower gold) - shorter duration
            quests.Add(GenerateQuestForLevel(playerLevel, QuestFocus.Experience, isLonger: false));
            
            // Quest 3: Balanced (equal gold and XP) - longer duration
            quests.Add(GenerateQuestForLevel(playerLevel, QuestFocus.Balanced, isLonger: true));
            
            Console.WriteLine($"[DEBUG] GetAvailableQuests - Generated {quests.Count} quests for level {playerLevel}:");
            foreach (var quest in quests)
            {
                Console.WriteLine($"[DEBUG] GetAvailableQuests - Quest: '{quest.Name}' (Type: {quest.Type}, Duration: {quest.Duration}min, XP: {quest.ExperienceReward}, Gold: {quest.GoldReward}, GrantsItem: {quest.GrantsItem})");
            }
            
            return quests;
        }

        private enum QuestFocus
        {
            Gold,
            Experience,
            Balanced
        }

        private Quest GenerateQuestForLevel(int playerLevel, QuestFocus focus, bool isLonger)
        {
            var random = new Random(Environment.TickCount + _random.Next(1000));
            
            var questTypes = new[] { QuestType.Adventure, QuestType.Work, QuestType.Arena };
            var type = questTypes[random.Next(questTypes.Length)];
            
            var (name, description) = GenerateQuestDetails(type);
            
            var baseDurationSeconds = 60;
            var levelScalingSeconds = 30;
            var baseCalculatedSeconds = Math.Max(60, baseDurationSeconds + (playerLevel - 1) * levelScalingSeconds);
            
            var variation = random.Next(-40, 41);
            var durationVariationSeconds = (int)(baseCalculatedSeconds * variation / 100.0);
            var totalSeconds = Math.Max(30, baseCalculatedSeconds + durationVariationSeconds);
            if (isLonger)
                totalSeconds = (int)(totalSeconds * 1.75);
            
            var duration = Math.Round(totalSeconds / 60.0, 2);
            
            var (experienceReward, goldReward) = GenerateRewardsForFocus(focus, (int)Math.Ceiling(duration), playerLevel);
            
            bool grantsItem = random.Next(3) == 0;
            Item grantedItem = null;
            if (grantsItem)
            {
                experienceReward = (int)Math.Round(experienceReward * 0.5);
                goldReward = (int)Math.Round(goldReward * 0.5);
                grantedItem = ItemsController.GenerateRandomItem(playerLevel);
                Console.WriteLine($"[DEBUG] Generated quest item for quest '{name}' (Level {playerLevel}): {grantedItem.Name} (Type: {grantedItem.Type}, MinDamage: {grantedItem.MinDamage}, MaxDamage: {grantedItem.MaxDamage}, STR: {grantedItem.StrengthBonus}, DEX: {grantedItem.DexterityBonus}, INT: {grantedItem.IntelligenceBonus}, CON: {grantedItem.ConstitutionBonus}, LUCK: {grantedItem.LuckBonus}, Armor: {grantedItem.Armor})");
            }

            return new Quest
            {
                Id = random.Next(1000, 9999),
                Name = name,
                Description = description,
                Duration = duration,
                ExperienceReward = experienceReward,
                GoldReward = goldReward,
                RequiredLevel = playerLevel,
                Type = type,
                PlayerId = null,
                StartedAt = null,
                IsCompleted = false,
                GrantsItem = grantsItem,
                GrantedItem = grantedItem,
                GrantedItemJson = grantedItem != null ? System.Text.Json.JsonSerializer.Serialize(grantedItem) : ""
            };
        }

        private (int experienceReward, int goldReward) GenerateRewardsForFocus(QuestFocus focus, int duration, int playerLevel)
        {
            double baseXP = (300 * Math.Pow(playerLevel, 0.66) * duration) / 2.0; 
            int xp = (int)Math.Round(baseXP);
            int gold = 12 * playerLevel * duration;
            (int xpFinal, int goldFinal) = focus switch
            {
                QuestFocus.Gold => (xp / 2, gold * 2), 
                QuestFocus.Experience => (xp * 2, gold / 2),
                QuestFocus.Balanced => (xp, gold),
                _ => (xp, gold)
            };
            var random = new Random();
            double xpNoise = 1 + (random.NextDouble() * 0.3 - 0.15);
            double goldNoise = 1 + (random.NextDouble() * 0.3 - 0.15);
            xpFinal = (int)Math.Round(xpFinal * xpNoise);
            goldFinal = (int)Math.Round(goldFinal * goldNoise);
            return (xpFinal, goldFinal);
        }

        private (string name, string description) GenerateQuestDetails(QuestType type)
        {
            return type switch
            {
                QuestType.Adventure => GenerateAdventureQuest(),
                QuestType.Work => GenerateWorkQuest(),
                QuestType.Arena => GenerateArenaQuest(),
                _ => ("Mysterious Quest", "A quest of unknown origin.")
            };
        }

        private (string name, string description) GenerateAdventureQuest()
        {
            var names = new[]
            {
                "Goblin Hunt", "Dragon Slaying", "Treasure Hunt", "Monster Extermination",
                "Cave Exploration", "Forest Patrol", "Mountain Climb", "Swamp Cleanup",
                "Bandit Raid", "Ancient Ruins", "Mystic Portal", "Beast Tracking"
            };
            
            var descriptions = new[]
            {
                "Venture into dangerous territory to hunt down troublesome creatures.",
                "Face a mighty dragon that has been terrorizing the countryside.",
                "Search for hidden treasures in ancient locations.",
                "Clear out monsters that have been causing problems for locals.",
                "Explore mysterious caves for valuable resources.",
                "Patrol the forest to ensure the safety of travelers.",
                "Climb treacherous mountains to reach hidden locations.",
                "Clean up the swamp of dangerous creatures.",
                "Raid bandit camps to restore order to the region.",
                "Explore ancient ruins for lost knowledge and artifacts.",
                "Investigate a mysterious portal that has appeared.",
                "Track down a dangerous beast that has been spotted."
            };

            var index = _random.Next(names.Length);
            return (names[index], descriptions[index]);
        }

        private (string name, string description) GenerateWorkQuest()
        {
            var names = new[]
            {
                "Tavern Work", "Blacksmith Assistant", "Merchant Guard", "Farm Hand",
                "Mine Worker", "Lumberjack", "Fisherman", "Herbalist",
                "Messenger", "Cleaner", "Cook Assistant", "Stable Hand"
            };
            
            var descriptions = new[]
            {
                "Help out at the local tavern with various tasks.",
                "Assist the blacksmith with crafting and repairs.",
                "Guard a merchant caravan on their journey.",
                "Help with farm work and harvesting crops.",
                "Work in the mines to extract valuable minerals.",
                "Cut down trees and gather lumber for construction.",
                "Fish in local waters to provide food for the community.",
                "Gather herbs and medicinal plants from the wilderness.",
                "Deliver important messages across the region.",
                "Clean and maintain various buildings and facilities.",
                "Assist the cook with preparing meals for many people.",
                "Take care of horses and maintain the stables."
            };

            var index = _random.Next(names.Length);
            return (names[index], descriptions[index]);
        }

        private (string name, string description) GenerateArenaQuest()
        {
            var names = new[]
            {
                "Arena Training", "Gladiator Combat", "Tournament Entry", "Sparring Match",
                "Combat Practice", "Weapon Mastery", "Fighting Challenge", "Battle Trial",
                "Combat Assessment", "Warrior's Test", "Fighting Tournament", "Combat Training"
            };
            
            var descriptions = new[]
            {
                "Train in the arena to improve combat skills.",
                "Participate in gladiator-style combat for glory.",
                "Enter a tournament to prove your worth.",
                "Engage in friendly sparring matches with other warriors.",
                "Practice combat techniques with experienced fighters.",
                "Master the use of various weapons in combat.",
                "Accept a challenge from a skilled opponent.",
                "Undergo a trial to test your battle prowess.",
                "Have your combat abilities assessed by experts.",
                "Take the warrior's test to prove your mettle.",
                "Compete in a fighting tournament for rewards.",
                "Receive specialized combat training from masters."
            };

            var index = _random.Next(names.Length);
            return (names[index], descriptions[index]);
        }

        // POST: api/Quests/5/start/{playerId}
        [HttpPost("{questId}/start/{playerId}")]
        public async Task<ActionResult<Quest>> StartQuest(int questId, int playerId)
        {
            Console.WriteLine($"[DEBUG] StartQuest called - QuestId: {questId}, PlayerId: {playerId}");
            
            var player = await _context.Players
                .Include(p => p.ActiveQuests)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                Console.WriteLine($"[DEBUG] StartQuest failed - Player not found: {playerId}");
                return NotFound("Player not found");
            }

            Console.WriteLine($"[DEBUG] StartQuest - Found player: {player.Name} (Level {player.Level}, Class {player.Class})");

            // Check if player already has an active quest
            var activeQuests = await _context.Quests
                .Where(q => q.PlayerId == playerId && !q.IsCompleted)
                .ToListAsync();

            if (activeQuests.Any())
            {
                Console.WriteLine($"[DEBUG] StartQuest failed - Player {playerId} already has {activeQuests.Count} active quest(s)");
                return BadRequest("You can only have one active quest at a time. Please complete your current quest first.");
            }

            Console.WriteLine($"[DEBUG] StartQuest - No active quests found for player {playerId}");

            // Generate a new quest based on the questId (which is now just a seed for generation)
            var quest = GenerateQuestFromSeed(questId, player.Level);
            Console.WriteLine($"[DEBUG] StartQuest - Generated quest: '{quest.Name}' (Type: {quest.Type}, Duration: {quest.Duration}min, XP: {quest.ExperienceReward}, Gold: {quest.GoldReward})");

            if (player.Level < quest.RequiredLevel)
            {
                Console.WriteLine($"[DEBUG] StartQuest failed - Player level {player.Level} too low for quest requiring level {quest.RequiredLevel}");
                return BadRequest("Player level too low");
            }

            // Create new active quest instance
            var activeQuest = new Quest
            {
                Name = quest.Name,
                Description = quest.Description,
                Duration = quest.Duration,
                ExperienceReward = quest.ExperienceReward,
                GoldReward = quest.GoldReward,
                RequiredLevel = quest.RequiredLevel,
                Type = quest.Type,
                PlayerId = playerId,
                StartedAt = DateTime.UtcNow,
                GrantsItem = quest.GrantsItem,
                GrantedItem = quest.GrantedItem,
                GrantedItemJson = quest.GrantedItemJson
            };
            
            Console.WriteLine($"[DEBUG] StartQuest - Created active quest instance with ID: {activeQuest.Id}");
            
            // Generate and persist the enemy for this quest
            var enemy = GenerateEnemyForQuest(activeQuest);
            activeQuest.Enemy = enemy;
            Console.WriteLine($"[DEBUG] StartQuest - Generated enemy: {enemy.Name} (Level {enemy.Level}, HP: {enemy.HitPoints}, Damage: {enemy.MinDamage}-{enemy.MaxDamage})");
            
            // Set GrantedItemJson BEFORE saving
            activeQuest.GrantedItemJson = activeQuest.GrantedItem != null ? System.Text.Json.JsonSerializer.Serialize(activeQuest.GrantedItem) : "";

            _context.Quests.Add(activeQuest);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[DEBUG] StartQuest - Successfully saved quest to database. Quest ID: {activeQuest.Id}");
            Console.WriteLine($"[DEBUG] StartQuest - Quest started at: {activeQuest.StartedAt}, Expected completion: {activeQuest.StartedAt?.AddMinutes(activeQuest.Duration)}");

            return activeQuest;
        }

        private Quest GenerateQuestFromSeed(int seed, int playerLevel)
        {
            // Use the seed to generate a deterministic quest
            var random = new Random(seed);
            var questTypes = new[] { QuestType.Adventure, QuestType.Work, QuestType.Arena };
            var type = questTypes[random.Next(questTypes.Length)];
            
            var (name, description) = GenerateQuestDetails(type);
            
            // Generate level-scaled duration based on player level
            var baseDurationSeconds = 60; // 1 minute for level 1
            var levelScalingSeconds = 30; // 30 seconds per level increase
            var baseCalculatedSeconds = Math.Max(60, baseDurationSeconds + (playerLevel - 1) * levelScalingSeconds);
            
            // Add variation: ±40% random noise for more variation
            var variation = random.Next(-40, 41); // -40% to +40%
            var durationVariationSeconds = (int)(baseCalculatedSeconds * variation / 100.0);
            var totalSeconds = Math.Max(30, baseCalculatedSeconds + durationVariationSeconds);
            
            // Generate rewards based on seed to determine focus
            var focusType = seed % 3; // 0 = Gold, 1 = XP, 2 = Balanced
            var (experienceReward, goldReward) = focusType switch
            {
                0 => GenerateRewardsForFocus(QuestFocus.Gold, (int)Math.Ceiling(totalSeconds / 60.0), playerLevel),
                1 => GenerateRewardsForFocus(QuestFocus.Experience, (int)Math.Ceiling(totalSeconds / 60.0), playerLevel),
                _ => GenerateRewardsForFocus(QuestFocus.Balanced, (int)Math.Ceiling(totalSeconds / 60.0), playerLevel)
            };

            // Randomly mark about 1 in 3 quests as GrantsItem, and reduce rewards by 50%
            bool grantsItem = random.Next(3) == 0;
            Item grantedItem = null;
            if (grantsItem)
            {
                experienceReward = (int)Math.Round(experienceReward * 0.5);
                goldReward = (int)Math.Round(goldReward * 0.5);
                grantedItem = ItemsController.GenerateRandomItem(playerLevel);
                Console.WriteLine($"[DEBUG] Generated quest item for quest '{name}' (Level {playerLevel}): {grantedItem.Name} (Type: {grantedItem.Type}, MinDamage: {grantedItem.MinDamage}, MaxDamage: {grantedItem.MaxDamage}, STR: {grantedItem.StrengthBonus}, DEX: {grantedItem.DexterityBonus}, INT: {grantedItem.IntelligenceBonus}, CON: {grantedItem.ConstitutionBonus}, LUCK: {grantedItem.LuckBonus}, Armor: {grantedItem.Armor})");
            }

            return new Quest
            {
                Id = seed,
                Name = name,
                Description = description,
                Duration = Math.Round(totalSeconds / 60.0, 2),
                ExperienceReward = experienceReward,
                GoldReward = goldReward,
                RequiredLevel = playerLevel,
                Type = type,
                PlayerId = null,
                StartedAt = null,
                IsCompleted = false,
                GrantsItem = grantsItem,
                GrantedItem = grantedItem,
                GrantedItemJson = grantedItem != null ? System.Text.Json.JsonSerializer.Serialize(grantedItem) : ""
            };
        }

        // POST: api/Quests/5/complete
        [HttpPost("{questId}/complete")]
        public async Task<ActionResult<object>> CompleteQuest(int questId)
        {
            Console.WriteLine($"[DEBUG] CompleteQuest called - QuestId: {questId}");
            
            var quest = await _context.Quests
                .Include(q => q.Player)
                .ThenInclude(p => p.Items)
                .FirstOrDefaultAsync(q => q.Id == questId);

            if (quest == null || quest.Player == null)
            {
                Console.WriteLine($"[DEBUG] CompleteQuest failed - Quest not found or player is null: {questId}");
                return NotFound();
            }

            Console.WriteLine($"[DEBUG] CompleteQuest - Found quest: '{quest.Name}' for player: {quest.Player.Name}");

            if (quest.IsCompleted)
            {
                Console.WriteLine($"[DEBUG] CompleteQuest failed - Quest {questId} already completed");
                return BadRequest("Quest already completed");
            }

            var timeElapsed = DateTime.UtcNow - quest.StartedAt.Value;
            Console.WriteLine($"[DEBUG] CompleteQuest - Time elapsed: {timeElapsed.TotalMinutes:F2} minutes, Required: {quest.Duration} minutes");
            
            if (timeElapsed.TotalMinutes < quest.Duration)
            {
                Console.WriteLine($"[DEBUG] CompleteQuest failed - Quest not finished yet. Remaining time: {quest.Duration - timeElapsed.TotalMinutes:F2} minutes");
                return BadRequest("Quest not finished yet");
            }

            Console.WriteLine($"[DEBUG] CompleteQuest - Quest duration satisfied, starting battle simulation");

            // --- Enemy Battle Logic ---
            var battleLog = new List<string>();
            var battleReplay = new List<BattleStep>();
            var enemy = quest.Enemy ?? GenerateEnemyForQuest(quest);
            var player = quest.Player;
            
            Console.WriteLine($"[DEBUG] CompleteQuest - Battle participants: Player '{player.Name}' vs Enemy '{enemy.Name}'");
            
            int playerLevel = player.Level;
            int playerConstitution = player.Constitution + player.ConstitutionUpgrades + player.Items
                .Where(i => i is { IsEquipped: true, ConstitutionBonus: > 0 })
                .Sum(x => x.ConstitutionBonus);
            int playerHP = playerConstitution * 5 * (playerLevel + 1);
            battleLog.Add($"Player HP calculation: (Constitution {player.Constitution} + Equipped CON Bonus {player.Items.Where(i => i.IsEquipped).Sum(i => i.ConstitutionBonus)}) * 5 * (Level {playerLevel} + 1) = {playerHP}");
            
            Console.WriteLine($"[DEBUG] CompleteQuest - Player stats: Level {playerLevel}, Constitution {player.Constitution}, Equipped CON bonus: {player.Items.Where(i => i.IsEquipped).Sum(i => i.ConstitutionBonus)}, Total HP: {playerHP}");
            
            // Calculate enemy stats from base attributes
            int enemyHP = (enemy.Constitution) * 5 * (enemy.Level + 1);
            battleLog.Add($"Enemy HP calculation: Constitution {enemy.Constitution} * 5 * (Level {enemy.Level} + 1) = {enemyHP}");
            
            Console.WriteLine($"[DEBUG] CompleteQuest - Enemy stats: Level {enemy.Level}, Constitution {enemy.Constitution}, Total HP: {enemyHP}");
            
            double enemyStrengthFactor = 1 + enemy.Strength * 0.1;
            int enemyMinDamage = (int)Math.Round(enemy.Level * 2 * enemyStrengthFactor);
            int enemyMaxDamage = (int)Math.Round(enemy.Level * 4 * enemyStrengthFactor);
            battleLog.Add($"Enemy Damage calculation: Min {enemyMinDamage}, Max {enemyMaxDamage} (Strength Factor: {enemyStrengthFactor})");
            
            Console.WriteLine($"[DEBUG] CompleteQuest - Enemy damage: {enemyMinDamage}-{enemyMaxDamage} (Strength: {enemy.Strength}, Factor: {enemyStrengthFactor:F2})");
            
            int enemyArmor = 0;
            switch (player.Class)
            {
                case PlayerClass.Warrior:
                    enemyArmor = enemy.Strength * 2;
                    break;
                case PlayerClass.Scout:
                    enemyArmor = enemy.Dexterity * 2;
                    break;
                case PlayerClass.Mage:
                    enemyArmor = enemy.Intelligence * 2;
                    break;
                default:
                    enemyArmor = (int)Math.Round(enemy.Dexterity * 0.5 + enemy.Level * 0.5); // fallback
                    break;
            }
            battleLog.Add($"Enemy Armor calculation: {enemyArmor}");
            
            Console.WriteLine($"[DEBUG] CompleteQuest - Enemy armor: {enemyArmor} (based on player class: {player.Class})");
            
            double enemyCritChance = Math.Min((enemy.Luck * 5.0) / (200 * playerLevel), 0.5); // max 50%
            battleLog.Add($"Enemy Crit Chance: min(({enemy.Luck} * 5) / (200 * {playerLevel}), 0.5) = {enemyCritChance}");
            
            Console.WriteLine($"[DEBUG] CompleteQuest - Enemy crit chance: {enemyCritChance:P2} (Luck: {enemy.Luck})");
            
            // Store these for returning to frontend
            enemy.MinDamage = enemyMinDamage;
            enemy.MaxDamage = enemyMaxDamage;
            enemy.Armor = enemyArmor;
            enemy.CritChance = enemyCritChance;
            enemy.HitPoints = enemyHP;
            var random = new Random();

            Console.WriteLine($"[DEBUG] CompleteQuest - Starting battle simulation...");

            // Calculate player damage range and crit
            int playerMinDamage = 0, playerMaxDamage = 0, playerCrit = 0, playerArmor = 0;
            var weapon = player.Items.FirstOrDefault(i => i is { IsEquipped: true, Type: ItemType.Weapon });
            var playerStrength = player.Strength + player.StrengthUpgrades + player.Items
                .Where(i => i is { IsEquipped: true, StrengthBonus: > 0 })
                .Sum(x => x.StrengthBonus);
            var playerDexterity = player.Dexterity + player.DexterityUpgrades + player.Items
                .Where(i => i is { IsEquipped: true, DexterityBonus: > 0 })
                .Sum(x => x.DexterityBonus);
            var playerIntelligence = player.Intelligence + player.IntelligenceUpgrades + player.Items
                .Where(i => i is { IsEquipped: true, IntelligenceBonus: > 0 })
                .Sum(x => x.IntelligenceBonus);
            var primaryStat = 0;
            switch (player.Class) {
                case PlayerClass.Warrior: // Warrior - uses Strength
                    primaryStat = playerStrength;
                break;
                case PlayerClass.Mage: // Mage - uses Intelligence
                    primaryStat = playerIntelligence;
                break;
                case PlayerClass.Scout: // Scout - uses Dexterity
                    primaryStat = playerDexterity;
                break;
            }
            battleLog.Add($"Player primary stat for damage: {primaryStat}");
            var damageMultiplier = 1 + (primaryStat * 0.1);
            battleLog.Add($"Player damage multiplier: 1 + ({primaryStat} * 0.1) = {damageMultiplier}");
            if (weapon != null)
            {
                var minDamage = Math.Floor(weapon.MinDamage * damageMultiplier);
                var maxDamage = Math.Floor(weapon.MaxDamage * damageMultiplier);
                playerMinDamage = (int)minDamage;
                playerMaxDamage = (int)maxDamage;
                battleLog.Add($"Player weapon: {weapon.Name}, MinDamage: {weapon.MinDamage}, MaxDamage: {weapon.MaxDamage}");
                battleLog.Add($"Player damage after multiplier: Min {playerMinDamage}, Max {playerMaxDamage}");
            }
            else
            {
                battleLog.Add("Player has no weapon equipped! Damage will be 0.");
            }
            playerArmor = player.Items.Where(i => i.IsEquipped).Sum(i => i.Armor);
            battleLog.Add($"Player Armor: {playerArmor}");
            double playerCritChance = Math.Min((player.Luck * 5.0) / (200 * enemy.Level), 0.5); // max 50%
            battleLog.Add($"Player Crit Chance: min(({player.Luck} * 5) / (200 * {enemy.Level}), 0.5) = {playerCritChance}");

            // Add initial step: both at full HP before any attack
            battleReplay.Add(new BattleStep {
                Attacker = "init",
                Damage = 0,
                Crit = false,
                TargetHP = playerHP, // Not used for init
                PlayerHP = playerHP,
                EnemyHP = enemyHP
            });

            // Battle loop
            bool playerTurn = true;
            int round = 1;
            while (playerHP > 0 && enemyHP > 0)
            {
                battleLog.Add($"--- Round {round} ---");
                if (playerTurn)
                {
                    int dmgRoll = random.Next(playerMinDamage, playerMaxDamage + 1);
                    battleLog.Add($"Player base damage roll: {dmgRoll}");
                    bool crit = random.NextDouble() < playerCritChance;
                    battleLog.Add($"Player crit roll: {crit}");
                    int dmgAfterCrit = crit ? dmgRoll * 2 : dmgRoll;
                    if (crit) battleLog.Add($"Player damage after crit: {dmgAfterCrit}");
                    int reduced = Math.Max(1, (int)(dmgAfterCrit * (1 - (double)enemyArmor / (100 * Math.Max(1, playerLevel)))));
                    battleLog.Add($"Enemy armor: {enemyArmor}, Player level: {playerLevel}, Damage after armor: {reduced}");
                    enemyHP -= reduced;
                    battleLog.Add($"Player attacks for {dmgAfterCrit}{(crit ? " (CRIT)" : "")}, reduced to {reduced}. Enemy HP: {Math.Max(0, enemyHP)}");
                    // Add to battleReplay
                    battleReplay.Add(new BattleStep {
                        Attacker = "player",
                        Damage = reduced,
                        Crit = crit,
                        TargetHP = Math.Max(0, enemyHP),
                        PlayerHP = Math.Max(0, playerHP),
                        EnemyHP = Math.Max(0, enemyHP)
                    });
                }
                else
                {
                    int dmgRoll = random.Next(enemyMinDamage, enemyMaxDamage + 1);
                    battleLog.Add($"Enemy base damage roll: {dmgRoll}");
                    bool crit = random.NextDouble() < enemyCritChance;
                    battleLog.Add($"Enemy crit roll: {crit}");
                    int dmgAfterCrit = crit ? dmgRoll * 2 : dmgRoll;
                    if (crit) battleLog.Add($"Enemy damage after crit: {dmgAfterCrit}");
                    int reduced = Math.Max(1, (int)(dmgAfterCrit * (1 - (double)playerArmor / (100 * Math.Max(1, enemy.Level)))));
                    battleLog.Add($"Player armor: {playerArmor}, Enemy level: {enemy.Level}, Damage after armor: {reduced}");
                    playerHP -= reduced;
                    battleLog.Add($"Enemy attacks for {dmgAfterCrit}{(crit ? " (CRIT)" : "")}, reduced to {reduced}. Player HP: {Math.Max(0, playerHP)}");
                    // Add to battleReplay
                    battleReplay.Add(new BattleStep {
                        Attacker = "enemy",
                        Damage = reduced,
                        Crit = crit,
                        TargetHP = Math.Max(0, playerHP),
                        PlayerHP = Math.Max(0, playerHP),
                        EnemyHP = Math.Max(0, enemyHP)
                    });
                }
                playerTurn = !playerTurn;
                round++;
            }

            bool playerWon = playerHP > 0;
            // Do NOT grant rewards or mark quest as completed here
            // Return the battle result and quest info
            return new { player = quest.Player, battleLog, battleReplay, playerWon, enemy, quest };
        }

        // POST: api/Quests/5/grant-rewards
        [HttpPost("{questId}/grant-rewards")]
        public async Task<ActionResult<object>> GrantQuestRewards(int questId)
        {
            var quest = await _context.Quests
                .Include(q => q.Player)
                .ThenInclude(p => p.Items)
                .FirstOrDefaultAsync(q => q.Id == questId);

            if (quest == null || quest.Player == null)
            {
                return NotFound();
            }

            if (quest.IsCompleted)
            {
                return BadRequest("Quest already completed");
            }

            // Grant rewards only if quest is finished
            var timeElapsed = DateTime.UtcNow - quest.StartedAt.Value;
            if (timeElapsed.TotalMinutes < quest.Duration)
            {
                return BadRequest("Quest not finished yet");
            }

            // Only grant rewards if player won (assume frontend tracks this from battle result)
            // For extra safety, you could store the battle result in the DB or require the frontend to pass it back
            bool playerWon = true; // Assume true for now (frontend should check)
            Item grantedItem = null;
            if (playerWon)
            {
                quest.IsCompleted = true;
                quest.Player.Experience += quest.ExperienceReward;
                quest.Player.Gold += quest.GoldReward;
                if (quest.GrantsItem == true)
                {
                    grantedItem = quest.GrantedItem;
                    if (grantedItem != null)
                    {
                        grantedItem.PlayerId = quest.PlayerId; // Assign to player
                        _context.Items.Add(grantedItem);
                        await _context.SaveChangesAsync();
                    }
                }
                // Auto level up if enough XP (using new formula)
                while (quest.Player.Experience >= GetRequiredExperienceForLevel(quest.Player.Level))
                {
                    quest.Player.Experience -= GetRequiredExperienceForLevel(quest.Player.Level);
                    quest.Player.Level++;
                }
                await _context.SaveChangesAsync();
            }
            else
            {
                quest.IsCompleted = true;
                await _context.SaveChangesAsync();
            }

            return new { player = quest.Player, grantedItem };
        }

        // Helper to generate an enemy for a quest
        private Enemy GenerateEnemyForQuest(Quest quest)
        {
            // Example: match by quest name or type
            var adventureEnemies = new[] { "Goblin", "Griffin", "Mountain Troll", "Bandit", "Wolf", "Tregandian Tribesman" };
            var workEnemies = new[] { "Angry Customer", "Drunk Patron", "Rogue Cow", "Mine Rat" };
            var arenaEnemies = new[] { "Arena Trainee", "Gladiator", "Veteran Fighter", "Champion" };
            var random = new Random();
            string name;
            switch (quest.Type)
            {
                case QuestType.Adventure:
                    name = adventureEnemies[random.Next(adventureEnemies.Length)];
                    break;
                case QuestType.Work:
                    name = workEnemies[random.Next(workEnemies.Length)];
                    break;
                case QuestType.Arena:
                    name = arenaEnemies[random.Next(arenaEnemies.Length)];
                    break;
                default:
                    name = "Mysterious Foe";
                    break;
            }
            int level = quest.RequiredLevel;
            return new Enemy
            {
                Name = name,
                Level = level,
                Strength = (int)Math.Round((8 + level * 0.4) * (0.4 +0.8 * random.NextDouble())),
                Dexterity = (int)Math.Round((8 + level * 0.4) * (0.4 +0.8 * random.NextDouble())),
                Intelligence = (int)Math.Round((8 + level * 0.4) * (0.4 +0.8 * random.NextDouble())),
                Constitution = (int)Math.Round((8 + level * 0.4) * (0.4 +0.8 * random.NextDouble())),
                Luck = (int)Math.Round((8 + level * 0.4) * (0.25 +random.NextDouble())) / 2
                // Do not set derived stats here
            };
        }

        // GET: api/Quests/player/5
        [HttpGet("player/{playerId}")]
        public async Task<ActionResult<IEnumerable<Quest>>> GetPlayerQuests(int playerId)
        {
            return await _context.Quests
                .Where(q => q.PlayerId == playerId && !q.IsCompleted)
                .ToListAsync();
        }

        // Add this endpoint to the controller
        [HttpPost("start/{playerId}")]
        public async Task<ActionResult<Quest>> StartQuestForPlayer(int playerId, [FromBody] StartQuestDto questDto)
        {
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Starting quest for player {playerId}");
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Quest DTO: ID={questDto.Id}, Name={questDto.Name}, Level={questDto.RequiredLevel}, Type={questDto.Type}");

            var player = await _context.Players
                .Include(p => p.ActiveQuests)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                Console.WriteLine($"[DEBUG] StartQuestForPlayer - Player {playerId} not found");
                return NotFound("Player not found");
            }

            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Found player: {player.Name}, Level: {player.Level}");

            var activeQuests = await _context.Quests
                .Where(q => q.PlayerId == playerId && !q.IsCompleted)
                .ToListAsync();

            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Active quests count: {activeQuests.Count}");

            if (activeQuests.Any())
            {
                Console.WriteLine($"[DEBUG] StartQuestForPlayer - Player already has active quests, rejecting request");
                return BadRequest("You can only have one active quest at a time.");
            }

            if (player.Level < questDto.RequiredLevel)
            {
                Console.WriteLine($"[DEBUG] StartQuestForPlayer - Player level {player.Level} too low for quest requiring level {questDto.RequiredLevel}");
                return BadRequest("Player level too low");
            }

            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Player level check passed");

            // Generate the quest using the same logic as available quests (use questDto.Id as seed)
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Generating quest from seed {questDto.Id}");
            var activeQuest = new Quest
            {
                Name = questDto.Name,
                Description = questDto.Description,
                Duration = questDto.Duration,
                ExperienceReward = questDto.ExperienceReward,
                GoldReward = questDto.GoldReward,
                RequiredLevel = questDto.RequiredLevel,
                Type = questDto.Type,
                PlayerId = playerId,
                StartedAt = DateTime.UtcNow,
                GrantsItem = questDto.GrantsItem,
                GrantedItem = questDto.GrantedItemJson != "" ? System.Text.Json.JsonSerializer.Deserialize<Item>(questDto.GrantedItemJson) : null,
                GrantedItemJson = questDto.GrantedItemJson
            };
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Generated quest: {activeQuest.Name}, Duration: {activeQuest.Duration}, XP: {activeQuest.ExperienceReward}, Gold: {activeQuest.GoldReward}");
            

            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Created quest entity with StartedAt: {activeQuest.StartedAt}");

            // Generate and persist the enemy for this quest
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Generating enemy for quest");
            var enemy = GenerateEnemyForQuest(activeQuest);
            activeQuest.Enemy = enemy;
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Generated enemy: {enemy.Name}, Level: {enemy.Level}, Stats: STR={enemy.Strength}, DEX={enemy.Dexterity}, INT={enemy.Intelligence}, CON={enemy.Constitution}, LUCK={enemy.Luck}");

            // Set GrantedItemJson BEFORE saving
            //activeQuest.GrantedItemJson = activeQuest.GrantedItem != null ? System.Text.Json.JsonSerializer.Serialize(activeQuest.GrantedItem) : "";
            //Console.WriteLine($"[DEBUG] StartQuestForPlayer - GrantedItemJson set: {(activeQuest.GrantedItem != null ? "Yes" : "No")}");

            _context.Quests.Add(activeQuest);
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Added quest to context, saving changes");
            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Quest started successfully with ID: {activeQuest.Id}");
            Console.WriteLine($"[DEBUG] StartQuestForPlayer - Quest will complete at: {activeQuest.StartedAt?.AddMinutes(activeQuest.Duration)}");

            return activeQuest;
        }

        // Add this method to calculate required XP for next level
        private int GetRequiredExperienceForLevel(int level)
        {
            return 10 * level * level * level + 10 * level * level + 50 * level + 330;
        }
    }
}