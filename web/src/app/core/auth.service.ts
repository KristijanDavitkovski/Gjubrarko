import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { AuthResponse } from './models';

const storageKey = 'gjubrarko.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<AuthResponse | null>(this.restore());

  readonly user = computed(() => this.session());
  readonly isSignedIn = computed(() => this.session() !== null);
  readonly isModerator = computed(() => {
    const roles = this.session()?.roles ?? [];
    return roles.includes('Moderator') || roles.includes('Admin');
  });

  get accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  login(email: string, password: string) {
    return this.http.post<AuthResponse>('/api/v1/auth/login', { email, password }).pipe(tap((r) => this.store(r)));
  }

  register(email: string, password: string, displayName: string) {
    return this.http
      .post<AuthResponse>('/api/v1/auth/register', { email, password, displayName })
      .pipe(tap((r) => this.store(r)));
  }

  logout() {
    localStorage.removeItem(storageKey);
    this.session.set(null);
  }

  private store(response: AuthResponse) {
    localStorage.setItem(storageKey, JSON.stringify(response));
    this.session.set(response);
  }

  private restore(): AuthResponse | null {
    try {
      const saved = JSON.parse(localStorage.getItem(storageKey) ?? 'null') as AuthResponse | null;
      return saved && new Date(saved.expiresAt) > new Date() ? saved : null;
    } catch {
      return null;
    }
  }
}
