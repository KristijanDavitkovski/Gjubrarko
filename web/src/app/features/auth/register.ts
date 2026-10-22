import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule],
  template: `
    <h1>Регистрација</h1>
    <form class="auth" [formGroup]="form" (ngSubmit)="submit()">
      <input formControlName="displayName" placeholder="Име што ќе се прикажува" autocomplete="nickname" />
      <input formControlName="email" type="email" placeholder="Е-пошта" autocomplete="email" />
      <input formControlName="password" type="password" placeholder="Лозинка (најмалку 8 знаци)" autocomplete="new-password" />
      @if (error()) {
        <p class="error">{{ error() }}</p>
      }
      <button class="primary" type="submit" [disabled]="form.invalid">Креирај профил</button>
    </form>
  `,
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly error = signal<string | null>(null);
  protected readonly form = inject(FormBuilder).nonNullable.group({
    displayName: ['', [Validators.required, Validators.maxLength(60)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected submit() {
    const { email, password, displayName } = this.form.getRawValue();
    this.auth.register(email, password, displayName).subscribe({
      next: () => this.router.navigate(['/']),
      error: (e) => this.error.set(Object.values(e.error?.errors ?? {}).flat().join(' ') || 'Регистрацијата не успеа.'),
    });
  }
}
