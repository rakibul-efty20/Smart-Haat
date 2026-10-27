import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiService } from './api.service';
import { Cart } from './models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly api = inject(ApiService);
  readonly cart = signal<Cart>({ items: [], subtotal: 0, currency: 'BDT' });
  readonly count = computed(() => this.cart().items.reduce((sum, item) => sum + item.quantity, 0));
  readonly loading = signal(false);
  async load(): Promise<Cart> { this.loading.set(true); try { const value = await firstValueFrom(this.api.get<Cart>('/api/cart')); this.cart.set(value); return value; } finally { this.loading.set(false); } }
  async refresh(): Promise<Cart> { const value = await firstValueFrom(this.api.get<Cart>('/api/cart')); this.cart.set(value); return value; }
  async add(productId: string, quantity = 1): Promise<void> { this.cart.set(await firstValueFrom(this.api.post<Cart>('/api/cart/items', { productId, quantity }))); }
  async set(productId: string, quantity: number): Promise<void> { this.cart.set(await firstValueFrom(this.api.put<Cart>(`/api/cart/items/${productId}`, { quantity }))); }
  async remove(productId: string): Promise<void> { this.cart.set(await firstValueFrom(this.api.delete<Cart>(`/api/cart/items/${productId}`))); }
  async merge(): Promise<void> { this.cart.set(await firstValueFrom(this.api.post<Cart>('/api/cart/merge', {}))); }
}
