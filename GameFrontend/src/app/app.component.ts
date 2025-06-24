import { Component, OnInit, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { GameService } from './services/game.service';
import { ApiService } from './services/api.service';
import { Player, UserDto, PlayerClass } from './models/game.models';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {CreatePlayerComponent} from './components/create-player/create-player.component';
import {LoginComponent} from './components/login/login.component';
import {RegisterComponent} from './components/register/register.component';
import {CharacterSelectComponent} from './components/character-select/character-select.component';
import {QuestBoardComponent} from './components/quest-board/quest-board.component';
import {InventoryComponent} from './components/inventory/inventory.component';
import {ShopComponent} from './components/shop/shop.component';
import {CommonModule} from '@angular/common';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  standalone: true,
  imports: [
    CreatePlayerComponent,
    LoginComponent,
    RegisterComponent,
    CharacterSelectComponent,
    QuestBoardComponent,
    InventoryComponent,
    ShopComponent,
    FormsModule,
    ReactiveFormsModule,
    CommonModule
  ],
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
    title = 'Shakes & Godmist';
    currentPlayer: Player | null = null;
    currentUser: UserDto | null = null;

    // Screen states
    showLogin = true;
    showRegister = false;
    showCharacterSelect = false;
    showCreatePlayer = false;
    showGame = false;

    // Tab management
    activeTab = 'stats';

    constructor(
        private gameService: GameService,
        private apiService: ApiService,
        @Inject(PLATFORM_ID) private platformId: Object
    ) {}

  ngOnInit(): void {
    console.log('AppComponent initializing...');

    // Check for existing authentication
    if (isPlatformBrowser(this.platformId)) {
      this.checkExistingAuth();
    }

    // Subscribe to authentication state
    this.apiService.currentUser$.subscribe(user => {
      this.currentUser = user;
      if (user) {
        // User is authenticated, show character selection
        this.showLogin = false;
        this.showRegister = false;
        this.showCharacterSelect = true;
        this.showCreatePlayer = false;
        this.showGame = false;
      } else {
        // No user, show login
        this.showLogin = true;
        this.showRegister = false;
        this.showCharacterSelect = false;
        this.showCreatePlayer = false;
        this.showGame = false;
        this.currentPlayer = null;
      }
    });

    // Subscribe to current player changes
    this.gameService.currentPlayer$.subscribe(player => {
      console.log('Current player changed:', player);
      this.currentPlayer = player;

      if (player) {
        this.showGame = true;
        this.showCharacterSelect = false;
      }
    });

    console.log('AppComponent initialization complete');
  }

  private checkExistingAuth(): void {
    const user = this.apiService.getCurrentUser();
    if (user) {
      this.currentUser = user;
      this.showLogin = false;
      this.showCharacterSelect = true;
    }
  }

  // Tab management
  setActiveTab(tab: string): void {
    this.activeTab = tab;
  }

  // Character info helpers
  getClassName(playerClass: PlayerClass): string {
    switch (playerClass) {
      case PlayerClass.Warrior: return 'Warrior';
      case PlayerClass.Mage: return 'Mage';
      case PlayerClass.Scout: return 'Scout';
      default: return 'Unknown';
    }
  }

  getXpPercentage(): number {
    if (!this.currentPlayer) return 0;
    const currentXp = this.currentPlayer.experience;
    const requiredXp = this.gameService.getExpRequiredForNextLevel(this.currentPlayer);
    return Math.min(100, Math.max(0, (currentXp / requiredXp) * 100));
  }

  getExpRequiredForCurrentLevel(): number {
    if (!this.currentPlayer) return 0;
    return this.gameService.getExpRequiredForNextLevel(this.currentPlayer);
  }

  // Authentication flow handlers
  onLoginSuccess(): void {
    // Login success is handled by the API service subscription
  }

  onRegistrationSuccess(): void {
    // Registration success is handled by the API service subscription
  }

  showRegisterScreen(): void {
    this.showLogin = false;
    this.showRegister = true;
  }

  showLoginScreen(): void {
    this.showRegister = false;
    this.showLogin = true;
  }

  // Character selection handlers
  onCharacterSelected(player: Player): void {
    this.gameService.setCurrentPlayer(player);
  }

  showCreateCharacter(): void {
    this.showCharacterSelect = false;
    this.showCreatePlayer = true;
  }

    onPlayerCreated(player: Player): void {
        this.gameService.setCurrentPlayer(player);
        this.showCreatePlayer = false;
    this.showGame = true;
    }

    logout(): void {
    this.apiService.logout();
        this.gameService.setCurrentPlayer(null as any);
    }
}
