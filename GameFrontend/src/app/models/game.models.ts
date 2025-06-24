export interface Player {
    id: number;
    name: string;
    level: number;
    experience: number;
    gold: number;
    strength: number;
    dexterity: number;
    intelligence: number;
    constitution: number;
    luck: number;
    class: PlayerClass;
    createdAt: Date;
    lastActive: Date;
    userId: number;
    items: Item[];
    activeQuests: Quest[];
    strengthUpgrades: number;
    dexterityUpgrades: number;
    intelligenceUpgrades: number;
    constitutionUpgrades: number;
    luckUpgrades: number;
}

export enum PlayerClass {
    Warrior = 1,
    Mage = 2,
    Scout = 3
}

export interface Item {
    id: number;
    name: string;
    description: string;
    type: ItemType;
    level: number;
    strengthBonus: number;
    dexterityBonus: number;
    intelligenceBonus: number;
    constitutionBonus: number;
    luckBonus: number;
    minDamage: number;
    maxDamage: number;
    armor: number;
    value: number;
    isEquipped: boolean;
    playerId?: number;
}

export enum ItemType {
    Weapon = 1,
    Helmet = 2,
    Armor = 3,
    Gloves = 4,
    Boots = 5,
    Shield = 6,
    Amulet = 7,
    Ring = 8
}

export interface Quest {
    id: number;
    name: string;
    description: string;
    duration: number;
    experienceReward: number;
    goldReward: number;
    requiredLevel: number;
    type: QuestType;
    playerId?: number;
    startedAt?: Date;
    isCompleted: boolean;
    grantedItem?: Item;
}

export enum QuestType {
    Adventure = 1,
    Work = 2,
    Arena = 3
}

export interface CreatePlayerDto {
    name: string;
    class: PlayerClass;
}

// Authentication models
export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
    username: string;
}

export interface AuthResponse {
    token: string;
    refreshToken: string;
    expiresAt: Date;
    user: UserDto;
}

export interface UserDto {
    id: number;
    email: string;
    username: string;
    characters: PlayerDto[];
}

export interface PlayerDto {
    id: number;
    name: string;
    level: number;
    class: PlayerClass;
    lastActive: Date;
}

export interface Enemy {
    name: string;
    level: number;
    strength: number;
    dexterity: number;
    intelligence: number;
    constitution: number;
    luck: number;
    minDamage: number;
    maxDamage: number;
    armor: number;
    critChance: number;
    hitPoints: number;
}