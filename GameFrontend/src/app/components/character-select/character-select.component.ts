import { Component, EventEmitter, Output, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { Player, PlayerClass } from '../../models/game.models';

@Component({
  selector: 'app-character-select',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './character-select.component.html',
  styleUrls: ['./character-select.component.css']
})
export class CharacterSelectComponent implements OnInit {
  @Output() characterSelected = new EventEmitter<Player>();
  @Output() createNewCharacter = new EventEmitter<void>();
  @Output() logoutClicked = new EventEmitter<void>();

  characters: Player[] = [];
  isLoading = true;
  error = '';

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadCharacters();
  }

  loadCharacters(): void {
    this.isLoading = true;
    this.error = '';

    this.apiService.getPlayers().subscribe({
      next: (characters) => {
        this.characters = characters;
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        // Don't show error for authentication issues - the interceptor will handle logout
        if (error.status !== 401 && error.status !== 403) {
          this.error = 'Failed to load characters. Please try again.';
          console.error('Error loading characters:', error);
        }
      }
    });
  }

  selectCharacter(character: Player): void {
    this.characterSelected.emit(character);
  }

  createCharacter(): void {
    this.createNewCharacter.emit();
  }

  logout(): void {
    this.logoutClicked.emit();
  }

  getClassName(playerClass: PlayerClass): string {
    switch (playerClass) {
      case PlayerClass.Warrior: return 'Warrior';
      case PlayerClass.Mage: return 'Mage';
      case PlayerClass.Scout: return 'Scout';
      default: return 'Unknown';
    }
  }

  getLastActiveText(lastActive: Date): string {
    const now = new Date();
    const lastActiveDate = new Date(lastActive);
    const diffInHours = Math.floor((now.getTime() - lastActiveDate.getTime()) / (1000 * 60 * 60));
    
    if (diffInHours < 1) return 'Just now';
    if (diffInHours < 24) return `${diffInHours} hours ago`;
    
    const diffInDays = Math.floor(diffInHours / 24);
    return `${diffInDays} days ago`;
  }
} 