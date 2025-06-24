using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ShakesGodmist.Models;

namespace ShakesFidgetClone.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlayersController : ControllerBase
    {
        private readonly GameContext _context;

        public PlayersController(GameContext context)
        {
            _context = context;
        }

        // GET: api/Players - Get current user's characters
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Player>>> GetMyPlayers()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            return await _context.Players
                .Where(p => p.UserId == userId)
                .Include(p => p.Items)
                .Include(p => p.ActiveQuests)
                .ToListAsync();
        }

        // GET: api/Players/5 - Get specific character (must be owned by current user)
        [HttpGet("{id}")]
        public async Task<ActionResult<Player>> GetPlayer(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var player = await _context.Players
                .Include(p => p.Items)
                .Include(p => p.ActiveQuests)
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (player == null)
            {
                return NotFound();
            }

            return player;
        }

        // POST: api/Players - Create new character for current user
        [HttpPost]
        public async Task<ActionResult<Player>> CreatePlayer(CreatePlayerDto playerDto)
        {
            Console.WriteLine($"[DEBUG] CreatePlayer called - Name: {playerDto.Name}, Class: {playerDto.Class}");
            
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                Console.WriteLine($"[DEBUG] CreatePlayer failed - No valid user ID found");
                return Unauthorized();
            }

            Console.WriteLine($"[DEBUG] CreatePlayer - User ID: {userId}");

            // Debug: Check if user exists in database
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId.Value);
            if (!userExists)
            {
                Console.WriteLine($"[DEBUG] CreatePlayer failed - User {userId.Value} does not exist in database");
                return BadRequest($"User with ID {userId.Value} does not exist in database");
            }

            // Check if character name is already taken by this user
            if (await _context.Players.AnyAsync(p => p.Name == playerDto.Name && p.UserId == userId))
            {
                Console.WriteLine($"[DEBUG] CreatePlayer failed - Character name '{playerDto.Name}' already exists for user {userId}");
                return BadRequest("Character name already exists for this user");
            }

            var player = new Player
            {
                Name = playerDto.Name,
                Class = playerDto.Class,
                UserId = userId.Value
            };

            _context.Players.Add(player);
            
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"[DEBUG] CreatePlayer - Player created successfully with ID: {player.Id}");
                
                // Add starter weapon based on class
                var starterWeapon = CreateStarterWeapon(playerDto.Class, player.Id);
                _context.Items.Add(starterWeapon);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[DEBUG] CreatePlayer - Starter weapon '{starterWeapon.Name}' added to player {player.Id}");
            }
            catch (Exception ex)
            {
                // Log the error details
                Console.WriteLine($"[DEBUG] CreatePlayer failed - Error: {ex.Message}");
                Console.WriteLine($"[DEBUG] CreatePlayer - User ID: {userId.Value}");
                Console.WriteLine($"[DEBUG] CreatePlayer - Player Name: {playerDto.Name}");
                Console.WriteLine($"[DEBUG] CreatePlayer - Player Class: {playerDto.Class}");
                throw;
            }

            return CreatedAtAction(nameof(GetPlayer), new { id = player.Id }, player);
        }

        // PUT: api/Players/5 - Update character (must be owned by current user)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlayer(int id, Player player)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            if (id != player.Id)
            {
                return BadRequest();
            }

            // Ensure user owns this character
            var existingPlayer = await _context.Players.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (existingPlayer == null)
            {
                return NotFound();
            }

            _context.Entry(player).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlayerExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Players/5 - Delete character (must be owned by current user)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlayer(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (player == null)
            {
                return NotFound();
            }

            _context.Players.Remove(player);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Players/5/levelup - Level up character (must be owned by current user)
        [HttpPost("{id}/levelup")]
        public async Task<ActionResult<Player>> LevelUp(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (player == null)
            {
                return NotFound();
            }

            int expRequired = player.Level * 100;
            if (player.Experience >= expRequired)
            {
                player.Level++;
                player.Experience -= expRequired;
                player.Strength += 2;
                player.Dexterity += 2;
                player.Intelligence += 2;
                player.Constitution += 2;
                player.Luck += 1;

                await _context.SaveChangesAsync();
            }

            return player;
        }

        // GET: api/Players/debug - Debug endpoint to check database state
        [HttpGet("debug")]
        public async Task<ActionResult<object>> DebugDatabase()
        {
            var userId = GetCurrentUserId();
            var allUsers = await _context.Users.Select(u => new { u.Id, u.Email, u.Username }).ToListAsync();
            var allPlayers = await _context.Players.Select(p => new { p.Id, p.Name, p.UserId }).ToListAsync();
            
            return new
            {
                CurrentUserId = userId,
                AllUsers = allUsers,
                AllPlayers = allPlayers
            };
        }

        // POST: api/Players/{id}/buy-stat
        [HttpPost("{id}/buy-stat")]
        public async Task<ActionResult<Player>> BuyStat(int id, [FromQuery] string stat)
        {
            Console.WriteLine($"[DEBUG] BuyStat called - PlayerId: {id}, Stat: {stat}");
            
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                Console.WriteLine($"[DEBUG] BuyStat failed - No valid user ID found");
                return Unauthorized();
            }

            var player = await _context.Players.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
            if (player == null)
            {
                Console.WriteLine($"[DEBUG] BuyStat failed - Player not found: {id} for user: {userId}");
                return NotFound();
            }

            Console.WriteLine($"[DEBUG] BuyStat - Found player: {player.Name} (Gold: {player.Gold})");

            // Track stat upgrades (add fields if not present)
            // We'll use reflection to avoid code duplication
            var validStats = new[] { "Strength", "Dexterity", "Intelligence", "Constitution", "Luck" };
            if (!validStats.Contains(stat))
            {
                Console.WriteLine($"[DEBUG] BuyStat failed - Invalid stat name: {stat}");
                return BadRequest("Invalid stat name");
            }

            // Get upgrade count property name
            var upgradePropName = stat + "Upgrades";
            var playerType = typeof(Player);
            var upgradeProp = playerType.GetProperty(upgradePropName);
            if (upgradeProp == null)
            {
                Console.WriteLine($"[DEBUG] BuyStat failed - Upgrade tracking not implemented for {stat}");
                return BadRequest($"Upgrade tracking for {stat} not implemented");
            }
            
            int upgrades = (int)(upgradeProp.GetValue(player) ?? 0);
            int price = (int)Math.Round(10 * Math.Pow(1.2, upgrades));
            
            Console.WriteLine($"[DEBUG] BuyStat - Current {stat} upgrades: {upgrades}, Price: {price} gold");
            
            if (player.Gold < price)
            {
                Console.WriteLine($"[DEBUG] BuyStat failed - Not enough gold. Player has: {player.Gold}, Upgrade costs: {price}");
                return BadRequest("Not enough gold");
            }
            
            // Deduct gold, increment stat and upgrade count
            player.Gold -= price;
            var statProp = playerType.GetProperty(stat);
            int oldStatValue = (int)statProp.GetValue(player);
            statProp.SetValue(player, oldStatValue + 1);
            upgradeProp.SetValue(player, upgrades + 1);
            
            Console.WriteLine($"[DEBUG] BuyStat - Transaction successful. {stat} increased from {oldStatValue} to {oldStatValue + 1}, Gold reduced to {player.Gold}");
            
            await _context.SaveChangesAsync();
            Console.WriteLine($"[DEBUG] BuyStat - Changes saved to database");
            
            return player;
        }

        private bool PlayerExists(int id)
        {
            var userId = GetCurrentUserId();
            return _context.Players.Any(e => e.Id == id && e.UserId == userId);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
        }

        private Item CreateStarterWeapon(PlayerClass playerClass, int playerId)
        {
            var random = new Random();
            
            switch (playerClass)
            {
                case PlayerClass.Warrior:
                    return new Item
                    {
                        Name = "Rusty Sword",
                        Description = "A basic sword that has seen better days, but still sharp enough to fight.",
                        Type = ItemType.Weapon,
                        Level = 1,
                        MinDamage = 3,
                        MaxDamage = 6,
                        StrengthBonus = 1,
                        Value = 10,
                        PlayerId = playerId,
                        IsEquipped = true
                    };
                    
                case PlayerClass.Mage:
                    return new Item
                    {
                        Name = "Apprentice Staff",
                        Description = "A simple wooden staff imbued with basic magical properties.",
                        Type = ItemType.Weapon,
                        Level = 1,
                        MinDamage = 2,
                        MaxDamage = 5,
                        IntelligenceBonus = 2,
                        Value = 10,
                        PlayerId = playerId,
                        IsEquipped = true
                    };
                    
                case PlayerClass.Scout:
                    return new Item
                    {
                        Name = "Hunting Bow",
                        Description = "A reliable bow used by hunters and scouts for ranged combat.",
                        Type = ItemType.Weapon,
                        Level = 1,
                        MinDamage = 4,
                        MaxDamage = 7,
                        DexterityBonus = 1,
                        Value = 10,
                        PlayerId = playerId,
                        IsEquipped = true
                    };
                    
                default:
                    return new Item
                    {
                        Name = "Basic Dagger",
                        Description = "A simple dagger for self-defense.",
                        Type = ItemType.Weapon,
                        Level = 1,
                        MinDamage = 2,
                        MaxDamage = 4,
                        Value = 5,
                        PlayerId = playerId,
                        IsEquipped = true
                    };
            }
        }
    }

    public class CreatePlayerDto
    {
        public string Name { get; set; }
        public PlayerClass Class { get; set; }
    }
}
