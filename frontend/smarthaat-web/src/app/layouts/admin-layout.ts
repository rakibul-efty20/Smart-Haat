import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { inject } from '@angular/core';

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="admin-shell">
      <aside class="admin-sidebar" [class.open]="menuOpen()">
        <a class="admin-brand" routerLink="/admin"><img src="/assets/smart-haat-brand.png" alt="Smart Haat"><span>MANAGEMENT</span></a>
        <div class="side-label">WORKSPACE</div>
        <nav class="admin-nav" aria-label="Administration">
          @if (auth.hasRole('Admin')) {
            <a routerLink="/admin/dashboard" routerLinkActive="selected"><span>▦</span> Overview</a>
            <a routerLink="/admin/products" routerLinkActive="selected"><span>◫</span> Products</a>
            <a routerLink="/admin/categories" routerLinkActive="selected"><span>▤</span> Categories</a>
            <a routerLink="/admin/inventory" routerLinkActive="selected"><span>▧</span> Inventory</a>
            <a routerLink="/admin/orders" routerLinkActive="selected"><span>▣</span> Orders</a>
            <a routerLink="/admin/delivery-zones" routerLinkActive="selected"><span>⌖</span> Delivery zones</a>
            <a routerLink="/admin/coupons" routerLinkActive="selected"><span>%</span> Coupons</a>
            <a routerLink="/admin/reports" routerLinkActive="selected"><span>▥</span> Reports</a>
          }
          @if (auth.hasRole('Salesman')) { <a routerLink="/admin/pos" routerLinkActive="selected"><span>▣</span> Point of sale</a> }
          @if (auth.hasRole('Salesman') && !auth.hasRole('Admin')) { <a routerLink="/admin/orders" routerLinkActive="selected"><span>▣</span> My sales</a><a routerLink="/admin/inventory" routerLinkActive="selected"><span>▧</span> Inventory</a> }
          <a routerLink="/" class="back-store"><span>←</span> Back to store</a>
        </nav>
        <div class="sidebar-user"><div class="avatar">{{ initials }}</div><div><strong>{{ auth.user()?.displayName }}</strong><small>{{ auth.user()?.roles?.join(' · ') }}</small></div><button type="button" title="Sign out" aria-label="Sign out" (click)="signOut()">↪</button></div>
      </aside>
      <div class="admin-main-wrap">
        <header class="admin-topbar"><button class="menu-toggle" type="button" aria-label="Toggle menu" (click)="toggleMenu()">☰</button><div><span class="admin-breadcrumb">SMART HAAT / ADMIN</span><h1>Store management</h1></div><a class="button button-outline button-sm" routerLink="/">View storefront ↗</a></header>
        <main class="admin-content"><router-outlet /></main>
      </div>
    </div>
  `
})
export class AdminLayout {
  readonly auth = inject(AuthService);
  readonly menuOpen = signal(false);
  toggleMenu(): void { this.menuOpen.update(value => !value); }
  get initials(): string { return (this.auth.user()?.displayName ?? 'A').split(/\s+/).map(x => x[0]).slice(0, 2).join('').toUpperCase(); }
  async signOut(): Promise<void> { await this.auth.logout().catch(() => undefined); location.href = '/'; }
}

