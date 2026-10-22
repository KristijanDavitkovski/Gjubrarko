import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <h1>Најава</h1>
    <form class="auth" [formGroup]="form" (ngSubmit)="submit()">
      <input formControlName="email" type="email" placeholder="Е-пошта" autocomplete="email" />
      <input formControlName="password" type="password" placeholder="Лозинка" autocomplete="current-password" />
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
      <button class="primary" type="submit" [disabled]="form.invalid">Најави се</button>
      <a routerLink="/register">Немате профил? Регистрирајте се</a>
    </form>
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly error = signal<string | null>(null);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  protected submit() {
    const { email, password } = this.form.getRawValue();
    this.auth.login(email, password).subscribe({
      next: () => this.router.navigate(['/']),
      error: () => this.error.set('Погрешна е-пошта или лозинка.'),
    });
  }
}
