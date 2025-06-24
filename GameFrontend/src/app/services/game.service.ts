import { Injectable } from '@angular/core';
import {BehaviorSubject, Observable, takeWhile, timer} from 'rxjs';
import { switchMap } from 'rxjs/operators';
import { Player, Quest } from '../models/game.models';
import { ApiService } from './api.service';
import { BattleStep } from '../services/api.service';
import { Enemy } from '../models/game.models';

export interface BattleReplayState {
    battleActive: boolean;
    battlePlayerHP: number;
    battleEnemyHP: number;
    battlePlayerMaxHP: number;
    battleEnemyMaxHP: number;
    battleEnemy: Enemy | null;
    battleReplay: BattleStep[];
    battleReplayIndex: number;
    lastPlayerDamage: number;
    lastEnemyDamage: number;
    lastPlayerCrit: boolean;
    lastEnemyCrit: boolean;
    currentBattleAction: 'player' | 'enemy' | '';
    battleOutcome: string;
}

@Injectable({
    providedIn: 'root'
})
export class GameService {
    private currentPlayerSubject = new BehaviorSubject<Player | null>(null);
    public currentPlayer$ = this.currentPlayerSubject.asObservable();

    private questTimerSubject = new BehaviorSubject<Quest[]>([]);
    public questTimer$ = this.questTimerSubject.asObservable();

    // Persistent quest result screen state
    public showResultScreen: boolean = false;
    public lastQuestXP: number = 0;
    public lastQuestGold: number = 0;
    public lastQuestName: string = '';
    public lastQuestOutcome: 'victory' | 'defeat' | '' = '';

    private _lastQuestId: number | null = null;
    setLastQuestId(id: number) {
        this._lastQuestId = id;
    }
    get lastQuestId(): number | null {
        return this._lastQuestId;
    }

    // Battle replay state
    private battleReplayState: BattleReplayState = {
        battleActive: false,
        battlePlayerHP: 0,
        battleEnemyHP: 0,
        battlePlayerMaxHP: 0,
        battleEnemyMaxHP: 0,
        battleEnemy: null,
        battleReplay: [],
        battleReplayIndex: 0,
        lastPlayerDamage: 0,
        lastEnemyDamage: 0,
        lastPlayerCrit: false,
        lastEnemyCrit: false,
        currentBattleAction: '',
        battleOutcome: ''
    };
    private battleReplayStateSubject = new BehaviorSubject<BattleReplayState>({...this.battleReplayState});
    public battleReplayState$ = this.battleReplayStateSubject.asObservable();
    private battleStepTimer?: any;
    private battlePaused: boolean = false;

    constructor(private apiService: ApiService) {
        console.log('[DEBUG] GameService - Initializing GameService');
    }

    setCurrentPlayer(player: Player): void {
        this.currentPlayerSubject.next(player);
        this.loadPlayerQuests(player.id);
    }

    restoreSession(playerId: number): void {
        this.apiService.getPlayer(playerId).subscribe({
            next: (player) => {
                this.setCurrentPlayer(player);
            },
            error: (error) => {
                console.error('Failed to restore session:', error);
                // Clear invalid session
                sessionStorage.removeItem('currentPlayerId');
            }
        });
    }

    getCurrentPlayer(): Player | null {
        return this.currentPlayerSubject.value;
    }

    refreshCurrentPlayer(): void {
        const currentPlayer = this.getCurrentPlayer();
        if (currentPlayer) {
            this.apiService.getPlayer(currentPlayer.id).subscribe(player => {
                this.setCurrentPlayer(player);
            });
        }
    }

    private loadPlayerQuests(playerId: number): void {
        console.log('[DEBUG] GameService - Loading quests for player:', playerId);
        this.apiService.getPlayerQuests(playerId).subscribe({
            next: (quests) => {
                console.log('[DEBUG] GameService - Loaded quests for player:', quests.length);
                this.questTimerSubject.next(quests);
            },
            error: (error) => {
                console.log('[DEBUG] GameService - Failed to load player quests:', error);
            }
        });
    }

    getQuestTimeRemaining(quest: Quest): string {
        if (!quest.startedAt) return '0:00';

        const startTime = new Date(quest.startedAt).getTime();
        const endTime = startTime + (quest.duration * 60 * 1000);
        const now = Date.now();
        const remaining = Math.max(0, endTime - now);

        const minutes = Math.floor(remaining / 60000);
        const seconds = Math.floor((remaining % 60000) / 1000);

        return `${minutes}:${seconds.toString().padStart(2, '0')}`;
    }

    isQuestCompleted(quest: Quest): boolean {
        if (!quest.startedAt) return false;

        const startTime = new Date(quest.startedAt).getTime();
        const endTime = startTime + (quest.duration * 60 * 1000);

        return Date.now() >= endTime;
    }

    canLevelUp(player: Player): boolean {
        const expRequired = this.getExpRequiredForNextLevel(player);
        return player.experience >= expRequired;
    }

    getExpRequiredForNextLevel(player: Player): number {
        const x = player.level;
        return 10 * x * x * x + 10 * x * x + 50 * x + 330;
    }

    getTotalStats(player: Player): any {
        const baseStats = {
            strength: player.strength + (player.strengthUpgrades || 0),
            dexterity: player.dexterity + (player.dexterityUpgrades || 0),
            intelligence: player.intelligence + (player.intelligenceUpgrades || 0),
            constitution: player.constitution + (player.constitutionUpgrades || 0),
            luck: player.luck + (player.luckUpgrades || 0)
        };

        const equippedItems = player.items.filter(item => item.isEquipped);

        return equippedItems.reduce((total, item) => ({
            strength: total.strength + item.strengthBonus,
            dexterity: total.dexterity + item.dexterityBonus,
            intelligence: total.intelligence + item.intelligenceBonus,
            constitution: total.constitution + item.constitutionBonus,
            luck: total.luck + item.luckBonus
        }), baseStats);
    }

    setQuestResultScreen(result: { xp: number, gold: number, name: string, outcome: 'victory' | 'defeat' | '' }) {
        console.log('[DEBUG] GameService - Setting quest result screen:', result);
        this.lastQuestXP = result.xp;
        this.lastQuestGold = result.gold;
        this.lastQuestName = result.name;
        this.lastQuestOutcome = result.outcome;
        this.showResultScreen = false; // Will be set to true after animation
    }

    closeResultScreen(): void {
        this.showResultScreen = false;
        this.lastQuestOutcome = '';
    }

    startBattleReplay(battleReplay: BattleStep[], enemy: Enemy | null): void {
        console.log('[DEBUG] GameService - Starting battle replay with', battleReplay.length, 'steps');
        console.log('[DEBUG] GameService - Enemy:', enemy?.name || 'null');
        
        // Initialize state
        this.battleReplayState = {
            battleActive: true,
            battlePlayerHP: 0,
            battleEnemyHP: 0,
            battlePlayerMaxHP: 0,
            battleEnemyMaxHP: 0,
            battleEnemy: enemy,
            battleReplay: battleReplay,
            battleReplayIndex: 0,
            lastPlayerDamage: 0,
            lastEnemyDamage: 0,
            lastPlayerCrit: false,
            lastEnemyCrit: false,
            currentBattleAction: '',
            battleOutcome: ''
        };
        // Set initial HPs from the first step if available
        if (battleReplay.length > 0 && battleReplay[0].attacker === 'init') {
            const initStep = battleReplay[0];
            if (initStep.playerHP !== undefined) {
                this.battleReplayState.battlePlayerHP = initStep.playerHP;
                this.battleReplayState.battlePlayerMaxHP = initStep.playerHP;
            }
            if (initStep.enemyHP !== undefined) {
                this.battleReplayState.battleEnemyHP = initStep.enemyHP;
                this.battleReplayState.battleEnemyMaxHP = initStep.enemyHP;
            }
            this.battleReplayState.battleReplayIndex = 1;
        }
        this.battlePaused = false;
        this.battleReplayStateSubject.next({...this.battleReplayState});
        this.playNextBattleStep();
    }

    private playNextBattleStep(): void {
        if (this.battlePaused) return;
        if (this.battleReplayState.battleReplayIndex >= this.battleReplayState.battleReplay.length) {
            this.battleReplayState.battleActive = false;
            this.battleReplayState.currentBattleAction = '';
            this.battleReplayStateSubject.next({...this.battleReplayState});
            // Show result screen immediately after animation ends
            if (this.lastQuestOutcome) {
                this.showResultScreen = true;
            }
            return;
        }
        const step = this.battleReplayState.battleReplay[this.battleReplayState.battleReplayIndex];
        this.battleReplayState.lastPlayerDamage = 0;
        this.battleReplayState.lastEnemyDamage = 0;
        this.battleReplayState.lastPlayerCrit = false;
        this.battleReplayState.lastEnemyCrit = false;
        if (step.attacker === 'player') {
            this.battleReplayState.currentBattleAction = 'player';
            this.battleReplayState.lastEnemyDamage = step.damage;
            this.battleReplayState.lastEnemyCrit = step.crit;
            this.battleReplayState.battleEnemyHP = step.targetHP;
        } else if (step.attacker === 'enemy') {
            this.battleReplayState.currentBattleAction = 'enemy';
            this.battleReplayState.lastPlayerDamage = step.damage;
            this.battleReplayState.lastPlayerCrit = step.crit;
            this.battleReplayState.battlePlayerHP = step.targetHP;
        } else {
            this.battleReplayState.currentBattleAction = '';
        }
        this.battleReplayState.battleReplayIndex++;
        this.battleReplayStateSubject.next({...this.battleReplayState});
        this.battleStepTimer = setTimeout(() => this.playNextBattleStep(), 1200);
    }

    pauseBattleReplay(): void {
        this.battlePaused = true;
        if (this.battleStepTimer) {
            clearTimeout(this.battleStepTimer);
        }
    }

    resumeBattleReplay(): void {
        if (!this.battleReplayState.battleActive || !this.battlePaused) return;
        this.battlePaused = false;
        this.playNextBattleStep();
    }

    stopBattleReplay(): void {
        this.battlePaused = false;
        if (this.battleStepTimer) {
            clearTimeout(this.battleStepTimer);
        }
        this.battleReplayState = {
            battleActive: false,
            battlePlayerHP: 0,
            battleEnemyHP: 0,
            battlePlayerMaxHP: 0,
            battleEnemyMaxHP: 0,
            battleEnemy: null,
            battleReplay: [],
            battleReplayIndex: 0,
            lastPlayerDamage: 0,
            lastEnemyDamage: 0,
            lastPlayerCrit: false,
            lastEnemyCrit: false,
            currentBattleAction: '',
            battleOutcome: ''
        };
        this.battleReplayStateSubject.next({...this.battleReplayState});
    }

    getBattleReplayState(): BattleReplayState {
        return {...this.battleReplayState};
    }
}
