import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenKey = 'device-detection-auth';

  readonly session = signal<AuthResponse | null>(this.readSession());
  readonly authenticated = computed(() => {
    const value = this.session();
    return !!value && new Date(value.expiresAt).getTime() > Date.now();
  });

  login(username: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/auth/login', { username, password }).pipe(
      tap(response => this.store(response))
    );
  }

  refresh(): Observable<AuthResponse> {
    const refreshToken = this.session()?.refreshToken ?? '';
    return this.http.post<AuthResponse>('/api/auth/refresh', { refreshToken }).pipe(
      tap(response => this.store(response))
    );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    this.session.set(null);
    void this.router.navigateByUrl('/login');
  }

  accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  private store(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, JSON.stringify(response));
    this.session.set(response);
  }

  private readSession(): AuthResponse | null {
    try {
      const raw = localStorage.getItem(this.tokenKey);
      return raw ? JSON.parse(raw) as AuthResponse : null;
    } catch {
      localStorage.removeItem(this.tokenKey);
      return null;
    }
  }
}
