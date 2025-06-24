import { Component, EventEmitter, Output } from '@angular/core';
import {FormBuilder, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { Player, PlayerClass, CreatePlayerDto } from '../../models/game.models';
import {CommonModule} from '@angular/common';

@Component({
  selector: 'app-create-player',
  templateUrl: './create-player.component.html',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CommonModule
  ],
  styleUrls: ['./create-player.component.css']
})
export class CreatePlayerComponent {
    @Output() playerCreated = new EventEmitter<Player>();

    createPlayerForm: FormGroup;
    playerClasses = [
        { value: PlayerClass.Warrior, name: 'Warrior', description: 'Strong melee fighter' },
        { value: PlayerClass.Mage, name: 'Mage', description: 'Powerful magic user' },
        { value: PlayerClass.Scout, name: 'Scout', description: 'Agile and lucky' }
    ];
    isLoading = false;
    error = '';

    constructor(
        private fb: FormBuilder,
        private apiService: ApiService
    ) {
        this.createPlayerForm = this.fb.group({
            name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20)]],
            class: [PlayerClass.Warrior, Validators.required]
        });
    }

    onSubmit(): void {
        if (this.createPlayerForm.valid && !this.isLoading) {
            this.isLoading = true;
            this.error = '';

            const playerDto: CreatePlayerDto = {
                name: this.createPlayerForm.value.name,
                class: this.createPlayerForm.value.class
            };

            this.apiService.createPlayer(playerDto).subscribe({
                next: (player) => {
                    this.playerCreated.emit(player);
                    this.isLoading = false;
                },
                error: (error) => {
                    this.error = 'Failed to create player. Please try again.';
                    this.isLoading = false;
                }
            });
        }
    }
}
