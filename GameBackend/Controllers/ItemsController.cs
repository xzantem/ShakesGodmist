using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShakesGodmist.Models;

namespace ShakesFidgetClone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly GameContext _context;
        private static readonly Random _random = new Random();

        public ItemsController(GameContext context)
        {
            _context = context;
        }

        // GET: api/Items/player/5
        [HttpGet("player/{playerId}")]
        public async Task<ActionResult<IEnumerable<Item>>> GetPlayerItems(int playerId)
        {
            return await _context.Items
                .Where(i => i.PlayerId == playerId)
                .ToListAsync();
        }

        // GET: api/Items/shop
        [HttpGet("shop")]
        public async Task<ActionResult<IEnumerable<Item>>> GetShopItems([FromQuery] int playerLevel = 1)
        {
            // Get current shop items, ordered by ShopSlot
            var shopItems = await _context.Items
                .Where(i => i.IsShopItem && i.PlayerId == null)
                .OrderBy(i => i.ShopSlot)
                .ToListAsync();

            // If fewer than 6, generate new ones to fill up empty slots
            var usedSlots = shopItems.Select(i => i.ShopSlot).ToHashSet();
            int itemsToAdd = 6 - shopItems.Count;
            for (int i = 0; i < itemsToAdd; i++)
            {
                // Find the first unused slot
                int slot = Enumerable.Range(0, 6).First(s => !usedSlots.Contains(s));
                usedSlots.Add(slot);
                var item = GenerateRandomItem(playerLevel);
                item.IsShopItem = true;
                item.PlayerId = null;
                item.Id = 0; // Let EF generate the real ID
                item.ShopSlot = slot;
                _context.Items.Add(item);
                shopItems.Add(item);
            }
            if (itemsToAdd > 0)
                await _context.SaveChangesAsync();

            // Return only 6 items, ordered by ShopSlot
            return shopItems.OrderBy(i => i.ShopSlot).Take(6).ToList();
        }

        public static Item GenerateRandomItem(int playerLevel = 1)
        {
            var itemTypes = Enum.GetValues<ItemType>();
            var randomType = itemTypes[_random.Next(itemTypes.Length)];
            
            // Set item price to the gold earned from 5 minutes of questing at this level
            int basePrice = (playerLevel * 3) * 5;
            // Add random noise ±15%
            double priceNoise = 1 + (_random.NextDouble() * 0.75 - 0.25); // -0.25 to +0.50
            int itemPrice = (int)Math.Round(basePrice * priceNoise);
            
            var item = new Item
            {
                Id = _random.Next(10000, 99999), // Temporary ID for shop items
                Name = GenerateItemName(randomType),
                Description = GenerateItemDescription(randomType),
                Type = randomType,
                Level = playerLevel, // Use player's level instead of random
                Value = itemPrice,
                IsEquipped = false,
                PlayerId = null // Shop items don't belong to any player
            };

            // Add random stat bonuses based on item type
            switch (randomType)
            {
                case ItemType.Weapon:
                    // Add damage range for weapons (4-8 base, scales with level)
                    var baseMinDamage = 4 + (playerLevel / 3);
                    var baseMaxDamage = 8 + (playerLevel / 3);
                    item.MinDamage = _random.Next(baseMinDamage, baseMaxDamage);
                    item.MaxDamage = _random.Next(item.MinDamage, baseMaxDamage + 1);
                    // Stat boost distribution: commonly 1 large, uncommonly 2, rarely 3, rarely 0
                    var weaponStatCount = _random.Next(100);
                    var availableStats = new[] { "Strength", "Dexterity", "Intelligence", "Constitution", "Luck" };
                    var selectedStats = new List<string>();
                    
                    if (weaponStatCount < 60) // 60% chance for 1 stat
                    {
                        selectedStats.Add(availableStats[_random.Next(availableStats.Length)]);
                    }
                    else if (weaponStatCount < 85) // 25% chance for 2 stats
                    {
                        var stat1 = availableStats[_random.Next(availableStats.Length)];
                        selectedStats.Add(stat1);
                        var remainingStats = availableStats.Where(s => s != stat1).ToArray();
                        selectedStats.Add(remainingStats[_random.Next(remainingStats.Length)]);
                    }
                    else if (weaponStatCount < 95) // 10% chance for 3 stats
                    {
                        var shuffledStats = availableStats.OrderBy(x => _random.Next()).Take(3).ToArray();
                        selectedStats.AddRange(shuffledStats);
                    }
                    // 5% chance for 0 stats (no bonuses)
                    
                    // Apply selected stat bonuses
                    foreach (var stat in selectedStats)
                    {
                        var bonus = _random.Next(1, 11); // Large bonus for weapons
                        switch (stat)
                        {
                            case "Strength": item.StrengthBonus = bonus; break;
                            case "Dexterity": item.DexterityBonus = bonus; break;
                            case "Intelligence": item.IntelligenceBonus = bonus; break;
                            case "Constitution": item.ConstitutionBonus = bonus; break;
                            case "Luck": item.LuckBonus = bonus; break;
                        }
                    }
                    break;
                case ItemType.Armor:
                    // Add armor for armor pieces (3-6 base, scales with level)
                    item.Armor = _random.Next(3, 7) + (playerLevel / 2);
                    // Stat boost distribution: uncommonly 1 small, rarely 2, almost never 3, commonly 0
                    var armorStatCount = _random.Next(100);
                    var armorAvailableStats = new[] { "Strength", "Dexterity", "Intelligence", "Constitution", "Luck" };
                    var armorSelectedStats = new List<string>();
                    
                    if (armorStatCount < 30) // 30% chance for 1 small stat
                    {
                        armorSelectedStats.Add(armorAvailableStats[_random.Next(armorAvailableStats.Length)]);
                    }
                    else if (armorStatCount < 45) // 15% chance for 2 stats
                    {
                        var stat1 = armorAvailableStats[_random.Next(armorAvailableStats.Length)];
                        armorSelectedStats.Add(stat1);
                        var remainingStats = armorAvailableStats.Where(s => s != stat1).ToArray();
                        armorSelectedStats.Add(remainingStats[_random.Next(remainingStats.Length)]);
                    }
                    else if (armorStatCount < 50) // 5% chance for 3 stats
                    {
                        var shuffledStats = armorAvailableStats.OrderBy(x => _random.Next()).Take(3).ToArray();
                        armorSelectedStats.AddRange(shuffledStats);
                    }
                    // 50% chance for 0 stats (pure armor items)
                    
                    // Apply selected stat bonuses
                    foreach (var stat in armorSelectedStats)
                    {
                        var bonus = _random.Next(1, 6); // Small bonus for armor
                        switch (stat)
                        {
                            case "Strength": item.StrengthBonus = bonus; break;
                            case "Dexterity": item.DexterityBonus = bonus; break;
                            case "Intelligence": item.IntelligenceBonus = bonus; break;
                            case "Constitution": item.ConstitutionBonus = bonus; break;
                            case "Luck": item.LuckBonus = bonus; break;
                        }
                    }
                    break;
                case ItemType.Helmet:
                    item.IntelligenceBonus = _random.Next(1, 8);
                    item.ConstitutionBonus = _random.Next(1, 6);
                    // Add armor for helmets (1-3 base, scales with level)
                    item.Armor = _random.Next(1, 4) + (playerLevel / 4);
                    break;
                case ItemType.Gloves:
                    item.DexterityBonus = _random.Next(1, 8);
                    item.StrengthBonus = _random.Next(0, 4);
                    // Add armor for gloves (1-2 base, scales with level)
                    item.Armor = _random.Next(1, 3) + (playerLevel / 5);
                    break;
                case ItemType.Boots:
                    item.DexterityBonus = _random.Next(1, 8);
                    item.LuckBonus = _random.Next(0, 4);
                    // Add armor for boots (1-2 base, scales with level)
                    item.Armor = _random.Next(1, 3) + (playerLevel / 5);
                    break;
                case ItemType.Shield:
                    item.ConstitutionBonus = _random.Next(1, 8);
                    item.StrengthBonus = _random.Next(0, 4);
                    // Add armor for shields (2-4 base, scales with level)
                    item.Armor = _random.Next(2, 5) + (playerLevel / 3);
                    break;
                case ItemType.Amulet:
                    item.LuckBonus = _random.Next(1, 8);
                    item.IntelligenceBonus = _random.Next(0, 4);
                    break;
                case ItemType.Ring:
                    item.LuckBonus = _random.Next(1, 6);
                    item.DexterityBonus = _random.Next(0, 4);
                    break;
            }

            return item;
        }

        private static string GenerateItemName(ItemType type)
        {
            var prefixes = new[] { "Ancient", "Mystical", "Enchanted", "Legendary", "Rare", "Epic", "Magical", "Blessed", "Cursed", "Divine" };
            var suffixes = new[] { "of Power", "of Wisdom", "of Strength", "of Agility", "of Fortune", "of Protection", "of Destruction", "of Healing" };
            
            var baseNames = type switch
            {
                ItemType.Weapon => new[] { "Sword", "Axe", "Mace", "Dagger", "Bow", "Staff", "Wand", "Hammer" },
                ItemType.Helmet => new[] { "Helmet", "Crown", "Hood", "Cap", "Circlet", "Mask" },
                ItemType.Armor => new[] { "Armor", "Plate", "Robe", "Vest", "Chestplate", "Tunic" },
                ItemType.Gloves => new[] { "Gloves", "Gauntlets", "Bracers", "Handwraps" },
                ItemType.Boots => new[] { "Boots", "Shoes", "Greaves", "Sandals", "Slippers" },
                ItemType.Shield => new[] { "Shield", "Buckler", "Aegis", "Barrier" },
                ItemType.Amulet => new[] { "Amulet", "Pendant", "Talisman", "Necklace" },
                ItemType.Ring => new[] { "Ring", "Band", "Circlet", "Signet" },
                _ => new[] { "Item" }
            };

            var prefix = prefixes[_random.Next(prefixes.Length)];
            var baseName = baseNames[_random.Next(baseNames.Length)];
            var suffix = suffixes[_random.Next(suffixes.Length)];

            return $"{prefix} {baseName} {suffix}";
        }

        private static string GenerateItemDescription(ItemType type)
        {
            var descriptions = type switch
            {
                ItemType.Weapon => new[] 
                { 
                    "A finely crafted weapon that gleams with magical energy.",
                    "This weapon has seen countless battles and carries the weight of history.",
                    "Forged by master craftsmen, this weapon is both beautiful and deadly.",
                    "Ancient runes cover the surface, pulsing with arcane power."
                },
                ItemType.Helmet => new[] 
                { 
                    "A protective headpiece that enhances mental clarity.",
                    "This helmet is adorned with precious gems and protective enchantments.",
                    "Wearing this helmet grants the bearer enhanced awareness.",
                    "Crafted from rare materials, this helmet offers superior protection."
                },
                ItemType.Armor => new[] 
                { 
                    "Sturdy armor that provides excellent protection in battle.",
                    "This armor is lightweight yet offers remarkable defense.",
                    "Enchanted armor that seems to absorb incoming damage.",
                    "A masterwork of defensive craftsmanship."
                },
                ItemType.Gloves => new[] 
                { 
                    "These gloves enhance manual dexterity and precision.",
                    "Magical gloves that improve the wearer's touch and grip.",
                    "Crafted for both protection and enhanced performance.",
                    "These gloves seem to respond to the wearer's thoughts."
                },
                ItemType.Boots => new[] 
                { 
                    "These boots provide both comfort and enhanced mobility.",
                    "Magical boots that make the wearer lighter on their feet.",
                    "Crafted for speed and agility in any terrain.",
                    "These boots seem to guide the wearer's steps."
                },
                ItemType.Shield => new[] 
                { 
                    "A reliable shield that has protected many warriors.",
                    "This shield is both sturdy and beautifully decorated.",
                    "Enchanted to deflect attacks with supernatural precision.",
                    "A legendary shield that has survived countless battles."
                },
                ItemType.Amulet => new[] 
                { 
                    "This amulet radiates with protective magic.",
                    "An ancient talisman that brings good fortune to its bearer.",
                    "This amulet enhances the wearer's natural abilities.",
                    "A mystical pendant that seems to pulse with life."
                },
                ItemType.Ring => new[] 
                { 
                    "A magical ring that enhances the wearer's abilities.",
                    "This ring has been passed down through generations.",
                    "Enchanted to bring luck and fortune to its bearer.",
                    "A powerful ring that responds to its owner's will."
                },
                _ => new[] { "A mysterious item with unknown properties." }
            };

            return descriptions[_random.Next(descriptions.Length)];
        }

        // POST: api/Items/{itemId}/buy/{playerId}
        [HttpPost("{itemId}/buy/{playerId}")]
        public async Task<ActionResult<Player>> BuyItem(int itemId, int playerId)
        {
            Console.WriteLine($"[DEBUG] BuyItem called - ItemId: {itemId}, PlayerId: {playerId}");
            
            var player = await _context.Players
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                Console.WriteLine($"[DEBUG] BuyItem failed - Player not found: {playerId}");
                return NotFound("Player not found");
            }

            Console.WriteLine($"[DEBUG] BuyItem - Found player: {player.Name} (Gold: {player.Gold}, Items: {player.Items.Count})");

            var shopItem = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId && i.IsShopItem && i.PlayerId == null);
            if (shopItem == null)
            {
                Console.WriteLine($"[DEBUG] BuyItem failed - Shop item not found: {itemId}");
                return NotFound("Shop item not found");
            }

            Console.WriteLine($"[DEBUG] BuyItem - Found shop item: '{shopItem.Name}' (Type: {shopItem.Type}, Value: {shopItem.Value}, Slot: {shopItem.ShopSlot})");

            if (player.Gold < shopItem.Value)
            {
                Console.WriteLine($"[DEBUG] BuyItem failed - Not enough gold. Player has: {player.Gold}, Item costs: {shopItem.Value}");
                return BadRequest("Not enough gold");
            }

            Console.WriteLine($"[DEBUG] BuyItem - Purchase validated. Proceeding with transaction...");

            int slot = shopItem.ShopSlot;
            player.Gold -= shopItem.Value;
            shopItem.PlayerId = playerId;
            shopItem.IsShopItem = false;
            shopItem.ShopSlot = -1;
            _context.Items.Update(shopItem);

            Console.WriteLine($"[DEBUG] BuyItem - Updated shop item. Player gold reduced to: {player.Gold}");

            // Generate a new shop item to replace the bought one, in the same slot
            var newShopItem = GenerateRandomItem(player.Level);
            newShopItem.IsShopItem = true;
            newShopItem.PlayerId = null;
            newShopItem.Id = 0;
            newShopItem.ShopSlot = slot;
            _context.Items.Add(newShopItem);

            Console.WriteLine($"[DEBUG] BuyItem - Generated new shop item: '{newShopItem.Name}' (Type: {newShopItem.Type}, Value: {newShopItem.Value}) for slot {slot}");

            await _context.SaveChangesAsync();

            Console.WriteLine($"[DEBUG] BuyItem - Successfully saved to database");

            // Return updated player with new items
            var updatedPlayer = await _context.Players
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == playerId);

            Console.WriteLine($"[DEBUG] BuyItem - Transaction completed. Player now has {updatedPlayer.Items.Count} items and {updatedPlayer.Gold} gold");

            return updatedPlayer;
        }

        // POST: api/Items/5/equip
        [HttpPost("{itemId}/equip")]
        public async Task<ActionResult<Player>> EquipItem(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.Player)
                .ThenInclude(p => p.Items)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null || item.Player == null)
            {
                return NotFound();
            }

            if (item.IsEquipped)
            {
                // If already equipped, unequip it
                item.IsEquipped = false;
                await _context.SaveChangesAsync();
                // Return updated player with items
                return await _context.Players
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == item.Player.Id);
            }

            // Unequip items of same type
            var existingEquipped = item.Player.Items
                .Where(i => i.Type == item.Type && i.IsEquipped)
                .ToList();

            foreach (var equipped in existingEquipped)
            {
                equipped.IsEquipped = false;
            }

            item.IsEquipped = true;
            await _context.SaveChangesAsync();

            // Return updated player with items
            return await _context.Players
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.Id == item.Player.Id);
        }

        // POST: api/Items/5/sell
        [HttpPost("{itemId}/sell")]
        public async Task<ActionResult<Player>> SellItem(int itemId)
        {
            var item = await _context.Items
                .Include(i => i.Player)
                .FirstOrDefaultAsync(i => i.Id == itemId);

            if (item == null || item.Player == null)
            {
                return NotFound();
            }

            item.Player.Gold += item.Value / 2; // Sell for half price
            _context.Items.Remove(item);

            await _context.SaveChangesAsync();

            return item.Player;
        }

        // POST: api/Items/refresh-shop/{playerId}
        [HttpPost("refresh-shop/{playerId}")]
        public async Task<ActionResult<IEnumerable<Item>>> RefreshShop(int playerId)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
                return NotFound("Player not found");
            int refreshCost = (player.Level * 3) * 5;
            if (player.Gold < refreshCost)
                return BadRequest($"Not enough gold to refresh shop. Cost: {refreshCost}");
            // Remove current shop items
            var shopItems = await _context.Items.Where(i => i.IsShopItem && i.PlayerId == null).ToListAsync();
            _context.Items.RemoveRange(shopItems);
            // Deduct gold
            player.Gold -= refreshCost;
            // Generate 6 new shop items
            var newShopItems = new List<Item>();
            for (int slot = 0; slot < 6; slot++)
            {
                var item = GenerateRandomItem(player.Level);
                item.IsShopItem = true;
                item.PlayerId = null;
                item.Id = 0;
                item.ShopSlot = slot;
                _context.Items.Add(item);
                newShopItems.Add(item);
            }
            await _context.SaveChangesAsync();
            return newShopItems.OrderBy(i => i.ShopSlot).ToList();
        }
    }
}