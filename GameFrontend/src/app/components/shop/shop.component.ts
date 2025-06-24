import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { GameService } from '../../services/game.service';
import { Item, Player } from '../../models/game.models';
import { ItemCardComponent } from '../item-card/item-card.component';

@Component({
  selector: 'app-shop',
  standalone: true,
  imports: [CommonModule, FormsModule, ItemCardComponent],
  templateUrl: './shop.component.html',
  styleUrls: ['./shop.component.css']
})
export class ShopComponent implements OnInit {
  shopItems: Item[] = [];
  currentPlayer: Player | null = null;
  loading = false;
  message = '';

  constructor(
    private apiService: ApiService,
    private gameService: GameService
  ) {}

  ngOnInit(): void {
    this.gameService.currentPlayer$.subscribe(player => {
      this.currentPlayer = player;
      if (player) {
        this.loadShopItems();
      }
    });
  }

  loadShopItems(): void {
    if (!this.currentPlayer) {
      this.loading = false;
      return;
    }

    this.loading = true;
    this.apiService.getShopItems(this.currentPlayer.level).subscribe({
      next: (items) => {
        this.shopItems = items;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading shop items:', error);
        this.message = 'Failed to load shop items';
        this.loading = false;
      }
    });
  }

  buyItem(item: Item): void {
    if (!this.currentPlayer) {
      this.message = 'No player selected';
      return;
    }

    if (this.currentPlayer.gold < item.value) {
      this.message = 'Not enough gold!';
      return;
    }

    this.loading = true;
    this.apiService.buyItem(item.id, this.currentPlayer.id).subscribe({
      next: (response) => {
        this.message = `Successfully purchased ${item.name}!`;
        // Update current player with new data from response
        this.gameService.setCurrentPlayer(response);
        // Refresh shop items to get a new random item
        this.loadShopItems();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error buying item:', error);
        this.message = 'Failed to purchase item';
        this.loading = false;
      }
    });
  }

  getItemTypeClass(item: Item): string {
    const itemType = String(item.type).toLowerCase();
    switch (itemType) {
      case 'weapon': return 'item-weapon';
      case 'armor': return 'item-armor';
      case 'helmet': return 'item-helmet';
      case 'gloves': return 'item-gloves';
      case 'boots': return 'item-boots';
      case 'shield': return 'item-shield';
      case 'amulet': return 'item-amulet';
      case 'ring': return 'item-ring';
      default: return 'item-other';
    }
  }

  refreshShop(): void {
    if (!this.currentPlayer) {
      this.message = 'No player selected';
      return;
    }
    if (this.currentPlayer.gold < 20) {
      this.message = 'Not enough gold to refresh the shop!';
      return;
    }
    this.loading = true;
    this.apiService.refreshShop(this.currentPlayer.id).subscribe({
      next: (items) => {
        this.shopItems = items;
        this.currentPlayer!.gold -= 20;
        this.message = 'Shop refreshed!';
        this.loading = false;
      },
      error: (error) => {
        this.message = error?.error || 'Failed to refresh shop';
        this.loading = false;
      }
    });
  }

  protected readonly String = String;
}
