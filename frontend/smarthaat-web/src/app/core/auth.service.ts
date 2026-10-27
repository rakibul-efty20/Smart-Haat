import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { CurrentUser, TokenResponse } from './models';

export class AuthProfileError extends Error {
  constructor(readonly originalError: unknown) { super('Login succeeded but the authenticated profile could not be loaded.'); }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  readonly token = signal<string | null>(null);
  readonly user = signal<CurrentUser | null>(null);
  readonly ready = signal(false);

  async restoreSession(): Promise<void> {
    try {
      const result = await firstValueFrom(this.api.post<TokenResponse>('/api/auth/refresh', {}));
      this.token.set(result.accessToken);
      this.user.set(await firstValueFrom(this.api.get<CurrentUser>('/api/auth/me')));
    } catch { this.token.set(null); this.user.set(null); }
    finally { this.ready.set(true); }
  }

  async login(email: string, password: string): Promise<CurrentUser> {
    const result = await firstValueFrom(this.api.post<TokenResponse>('/api/auth/login', { email, password }));
    this.token.set(result.accessToken);
    try {
      const user = await firstValueFrom(this.api.get<CurrentUser>('/api/auth/me'));
      this.user.set(user);
      return user;
    } catch (error) { this.token.set(null); this.user.set(null); throw new AuthProfileError(error); }
  }

  async register(displayName: string, email: string, password: string): Promise<void> {
    await firstValueFrom(this.api.post('/api/auth/register', { displayName, email, password }));
  }

  async logout(): Promise<void> {
    try { await firstValueFrom(this.api.post('/api/auth/logout', {})); }
    finally { this.token.set(null); this.user.set(null); }
  }

  hasRole(role: string): boolean { return this.user()?.roles?.includes(role) ?? false; }
  get signedIn(): boolean { return this.token() !== null; }
}
