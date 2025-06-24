import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Item, ItemType } from '../../models/game.models';

@Component({
  selector: 'app-item-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './item-card.component.html',
  styleUrls: ['./item-card.component.css']
})
export class ItemCardComponent {
  @Input() item: Item | undefined;

  getItemTypeString(type: ItemType | undefined): string {
    switch (type) {
      case ItemType.Weapon: return 'Weapon';
      case ItemType.Helmet: return 'Helmet';
      case ItemType.Armor: return 'Armor';
      case ItemType.Gloves: return 'Gloves';
      case ItemType.Boots: return 'Boots';
      case ItemType.Shield: return 'Shield';
      case ItemType.Amulet: return 'Amulet';
      case ItemType.Ring: return 'Ring';
      default: return '';
    }
  }
} 