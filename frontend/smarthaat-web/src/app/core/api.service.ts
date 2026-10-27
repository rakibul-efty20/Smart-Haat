import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl.replace(/\/$/, '');
  url(path: string): string { return path.startsWith('http') ? path : `${this.base}${path.startsWith('/') ? path : `/${path}`}`; }
  get<T>(path: string, query?: Record<string, string | number | boolean | null | undefined>) {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(query ?? {})) if (value !== null && value !== undefined && value !== '') params = params.set(key, String(value));
    return this.http.get<T>(this.url(path), { params, withCredentials: true });
  }
  post<T>(path: string, body: unknown, headers?: Record<string, string>) { return this.http.post<T>(this.url(path), body, { headers, withCredentials: true }); }
  put<T>(path: string, body: unknown) { return this.http.put<T>(this.url(path), body, { withCredentials: true }); }
  patch<T>(path: string, body: unknown) { return this.http.patch<T>(this.url(path), body, { withCredentials: true }); }
  delete<T>(path: string) { return this.http.delete<T>(this.url(path), { withCredentials: true }); }
  upload<T>(path: string, field: string, file: File) { const body = new FormData(); body.append(field, file); return this.http.post<T>(this.url(path), body, { withCredentials: true }); }
}
