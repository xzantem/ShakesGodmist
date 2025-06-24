import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { GameService, BattleReplayState } from '../../services/game.service';
import { Quest, Player, Enemy } from '../../models/game.models';
import { interval, Subscription, Subscription as RxSubscription } from 'rxjs';
import { BattleStep } from '../../services/api.service';
import { ItemCardComponent } from '../item-card/item-card.component';

@Component({
    selector: 'app-quest-board',
    templateUrl: './quest-board.component.html',
    standalone: true,
    styleUrls: ['./quest-board.component.css'],
    imports: [
      CommonModule,
      ItemCardComponent
    ]
})
export class QuestBoardComponent implements OnInit, OnDestroy {
    availableQuests: Quest[] = [];
    activeQuests: Quest[] = [];
    currentPlayer: Player | null = null;
    loading = false;
    message = '';
    battleLog: string[] = [];
    showDebugLog: boolean = false;
    activeQuestViewClosed: boolean = false;
    private progressTimerSubscription?: Subscription;
    private battleReplaySubscription?: RxSubscription;
    // Battle state from service
    battleState?: BattleReplayState;

    constructor(
        private apiService: ApiService,
        private gameService: GameService
    ) {}

    ngOnInit(): void {
        this.gameService.currentPlayer$.subscribe(player => {
            this.currentPlayer = player;
            if (player) {
                this.loadQuests();
            }
        });

        this.gameService.questTimer$.subscribe(quests => {
            this.activeQuests = quests;
            this.updateProgressTimer();
        });

        // Subscribe to battle state
        this.battleReplaySubscription = this.gameService.battleReplayState$.subscribe(state => {
            this.battleState = state;
        });

        // Resume battle if paused and active
        if (this.battleState?.battleActive) {
            this.gameService.resumeBattleReplay();
        }

        // Initialize battle state
        this.battleState = this.gameService.getBattleReplayState();
    }

    ngOnDestroy(): void {
        if (this.progressTimerSubscription) {
            this.progressTimerSubscription.unsubscribe();
        }
        if (this.battleReplaySubscription) {
            this.battleReplaySubscription.unsubscribe();
        }
        // Pause battle replay
        this.gameService.pauseBattleReplay();
    }

    private updateProgressTimer(): void {
        // Clear existing timer
        if (this.progressTimerSubscription) {
            this.progressTimerSubscription.unsubscribe();
        }

        // Start new timer if there are active quests
        if (this.activeQuests.length > 0) {
            this.progressTimerSubscription = interval(1000).subscribe(() => {
                // Force change detection by updating a property
                // This will trigger the progress bar to recalculate
                this.message = this.message; // Trigger change detection
            });
        }
    }

    loadQuests(): void {
        if (!this.currentPlayer) {
            this.loading = false;
            return;
        }

        this.loading = true;
        this.apiService.getAvailableQuests(this.currentPlayer.level).subscribe({
            next: (quests) => {
                this.availableQuests = quests;
                this.loading = false;
                console.log('Available quests:', this.availableQuests);
            },
            error: (error) => {
                console.error('Error loading quests:', error);
                this.message = 'Failed to load quests';
                this.loading = false;
            }
        });
    }

    startQuest(quest: Quest): void {
        this.activeQuestViewClosed = false;
        if (!this.currentPlayer) {
            this.message = 'No player selected';
            return;
        }

        this.loading = true;
        this.apiService.startQuest(quest, this.currentPlayer.id).subscribe({
                next: (activeQuest) => {
                this.message = `Started quest: ${quest.name}!`;
                this.loading = false;
                
                // Immediately refresh the player to get updated quest data
                this.gameService.refreshCurrentPlayer();
                
                // Also refresh available quests to remove the started quest
                    this.loadQuests();
                },
                error: (error) => {
                console.error('Error starting quest:', error);
                this.message = error.error || 'Failed to start quest';
                this.loading = false;
                }
            });
    }

    completeQuest(quest: Quest): void {
        this.activeQuestViewClosed = true;
        this.loading = true;
        this.apiService.completeQuest(quest.id).subscribe({
            next: (result) => {
                this.gameService.setCurrentPlayer(result.player);
                this.battleLog = result.battleLog || [];
                console.log('Battle replay:', result.battleReplay);
                // Start battle replay in service
                this.gameService.startBattleReplay(result.battleReplay || [], result.enemy || null);
                this.loading = false;
                this.gameService.refreshCurrentPlayer();
                this.loadQuests();
                // Store quest result for result screen in GameService
                this.gameService.setQuestResultScreen({
                    xp: quest.experienceReward,
                    gold: quest.goldReward,
                    name: quest.name,
                    outcome: result.playerWon ? 'victory' : 'defeat'
                });
                this.gameService.setLastQuestId(quest.id);
            },
            error: (error) => {
                console.error('Error completing quest:', error);
                this.message = error.error || 'Failed to complete quest';
                this.loading = false;
            }
        });
    }

    getTimeRemaining(quest: Quest): string {
        return this.gameService.getQuestTimeRemaining(quest);
    }

    isQuestCompleted(quest: Quest): boolean {
        return this.gameService.isQuestCompleted(quest);
    }

    getQuestProgressPercentage(quest: Quest): number {
        if (!quest.startedAt) return 0;
        
        const startTime = new Date(quest.startedAt).getTime();
        const currentTime = new Date().getTime();
        const durationMs = quest.duration * 60 * 1000; // Convert minutes to milliseconds
        const elapsed = currentTime - startTime;
        
        const percentage = Math.min(100, Math.max(0, (elapsed / durationMs) * 100));
        return Math.round(percentage);
    }

    formatDuration(minutes: number): string {
        // Convert decimal minutes to total seconds
        const totalSeconds = Math.round(minutes * 60);
        const mins = Math.floor(totalSeconds / 60);
        const secs = totalSeconds % 60;
        
        if (mins < 60) {
            return `${mins}:${secs.toString().padStart(2, '0')}`;
        } else {
            const hours = Math.floor(mins / 60);
            const remainingMinutes = mins % 60;
            return `${hours}:${remainingMinutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
        }
    }

    toggleDebugLog(): void {
        this.showDebugLog = !this.showDebugLog;
    }

    get showResultScreen(): boolean {
        return this.gameService.showResultScreen;
    }
    get lastQuestXP(): number {
        return this.gameService.lastQuestXP;
    }
    get lastQuestGold(): number {
        return this.gameService.lastQuestGold;
    }
    get lastQuestName(): string {
        return this.gameService.lastQuestName;
    }
    get lastQuestOutcome(): 'victory' | 'defeat' | '' {
        return this.gameService.lastQuestOutcome;
    }

    closeResultScreen(): void {
        // Only grant rewards if the player won
        if (this.gameService.lastQuestId && this.gameService.lastQuestOutcome === 'victory') {
            this.loading = true;
            this.apiService.grantQuestRewards(this.gameService.lastQuestId).subscribe({
                next: (result) => {
                    this.gameService.setCurrentPlayer(result.player);
                    this.loading = false;
                    this.activeQuestViewClosed = false;
                    this.gameService.closeResultScreen();
                    this.loadQuests();
                    this.gameService.refreshCurrentPlayer();
                },
                error: (error) => {
                    console.error('Error granting quest rewards:', error);
                    this.loading = false;
                    this.activeQuestViewClosed = false;
                    this.gameService.closeResultScreen();
                    this.gameService.refreshCurrentPlayer();
                }
            });
        } else {
            this.activeQuestViewClosed = false;
            this.gameService.closeResultScreen();
            this.gameService.refreshCurrentPlayer();
        }
    }

    // Add a helper to determine if quests can be started
    get canStartQuest(): boolean {
        return !this.showResultScreen && !this.battleState?.battleActive;
    }
}
