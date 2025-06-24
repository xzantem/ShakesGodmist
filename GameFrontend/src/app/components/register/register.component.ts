import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../services/api.service';
import { RegisterRequest } from '../../models/game.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  @Output() registrationSuccess = new EventEmitter<void>();
  @Output() showLogin = new EventEmitter<void>();

  registerForm: FormGroup;
  isLoading = false;
  error = '';
  registrationSuccessful = false;

  constructor(
    private fb: FormBuilder,
    private apiService: ApiService
  ) {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    }, { validators: this.passwordMatchValidator });
  }

  passwordMatchValidator(form: FormGroup) {
    const password = form.get('password')?.value;
    const confirmPassword = form.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.valid && !this.isLoading) {
      this.isLoading = true;
      this.error = '';

      const registerRequest: RegisterRequest = {
        email: this.registerForm.value.email,
        username: this.registerForm.value.username,
        password: this.registerForm.value.password
      };

      this.apiService.register(registerRequest).subscribe({
        next: () => {
          this.isLoading = false;
          this.registrationSuccessful = true;
          this.registrationSuccess.emit();
        },
        error: (error) => {
          this.isLoading = false;
          this.error = error.error?.message || 'Registration failed. Please try again.';
        }
      });
    }
  }

  goToLogin(): void {
    this.showLogin.emit();
  }

  resetForm(): void {
    this.registrationSuccessful = false;
    this.registerForm.reset();
    this.error = '';
  }
} 