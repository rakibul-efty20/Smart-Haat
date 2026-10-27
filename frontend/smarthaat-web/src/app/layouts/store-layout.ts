import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../core/api.service';
import { AuthService } from '../core/auth.service';
import { CartService } from '../core/cart.service';
import { Category } from '../core/models';

@Component({
  selector: 'app-store-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="announcement">
      <div class="container announcement-inner">
        <span>Fresh groceries. Thoughtfully selected for your home.</span>
        <span><strong>Cash on delivery</strong><i aria-hidden="true"></i> Prices in Bangladeshi taka</span>
      </div>
    </div>
    <header class="store-header" (keydown.escape)="closeCategories()">
      <div class="container nav-main">
        <a class="brand" routerLink="/" aria-label="Smart Haat home" (click)="closeCategories()">
          <img src="/assets/smart-haat-brand.png" alt="Smart Haat — Smart Haat grocery store">
        </a>
        <form class="nav-search" (submit)="$event.preventDefault(); search(searchBox.value)">
          <label class="sr-only" for="site-search">Search products</label>
          <input #searchBox id="site-search" type="search" placeholder="Search for rice, vegetables, beef...">
          <button type="submit" class="search-button" aria-label="Search products">
            <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="10.8" cy="10.8" r="6.8"></circle><path d="m16 16 4.5 4.5"></path></svg>
          </button>
        </form>
        <div class="nav-actions">
          <a class="nav-action account-link" [routerLink]="auth.signedIn ? '/account/orders' : '/login'" aria-label="Your account">
            <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="8" r="3.5"></circle><path d="M5 20a7 7 0 0 1 14 0"></path></svg>
            <span><small>{{ auth.signedIn ? 'Welcome back' : 'Sign in' }}</small><strong>{{ auth.signedIn ? (auth.user()?.displayName || 'Account') : 'My account' }}</strong></span>
          </a>
          <a routerLink="/cart" class="nav-action cart-link" aria-label="Shopping cart">
            <span class="cart-icon-wrap"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 4h2l2.1 11.1a2 2 0 0 0 2 1.6h8.7a2 2 0 0 0 1.9-1.4L22 8H6"></path><circle cx="10" cy="20" r="1"></circle><circle cx="18" cy="20" r="1"></circle></svg><span class="cart-count">{{ cart.count() }}</span></span>
            <span><small>Your basket</small><strong>Cart</strong></span>
          </a>
        </div>
      </div>
      <div class="store-nav-row">
        <div class="container store-nav-inner">
          <div class="category-menu-wrap">
            <button type="button" class="category-menu-trigger" [class.is-open]="categoriesOpen()" [attr.aria-expanded]="categoriesOpen()" (click)="toggleCategories()">
              <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M4 6h16M4 12h16M4 18h16"></path></svg>
              <span>Shop by category</span>
              <svg class="chevron" viewBox="0 0 24 24" aria-hidden="true"><path d="m7 10 5 5 5-5"></path></svg>
            </button>
            @if (categoriesOpen()) {
              <button class="category-menu-scrim" type="button" aria-label="Close categories" (click)="closeCategories()"></button>
              <div class="category-mega-menu">
                <div class="mega-menu-heading"><strong>Explore categories</strong><a routerLink="/shop" (click)="closeCategories()">View all products <span aria-hidden="true">→</span></a></div>
                <div class="mega-menu-content">
                  <div class="mega-parent-list" role="list" aria-label="Product categories">
                    @for (category of rootCategories; track category.id) {
                      <button type="button" class="mega-parent" [class.active]="activeCategoryId() === category.id" (mouseenter)="activeCategoryId.set(category.id)" (focus)="activeCategoryId.set(category.id)" (click)="activeCategoryId.set(category.id)">
                        <span>{{ category.name }}</span><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 18 6-6-6-6"></path></svg>
                      </button>
                    }
                    @if (!rootCategories.length) { <p class="muted mega-empty">Categories will appear here when the store catalog is ready.</p> }
                  </div>
                  <div class="mega-child-panel">
                    @if (activeCategory(); as parent) {
                      <div class="mega-child-heading"><span class="eyebrow">Browse {{ parent.name }}</span><a [routerLink]="['/shop']" [queryParams]="{categoryId: parent.id}" (click)="closeCategories()">Shop all {{ parent.name }} →</a></div>
                      @if (childrenOf(parent.id).length) {
                        <div class="mega-child-grid">@for (child of childrenOf(parent.id); track child.id) { <a [routerLink]="['/shop']" [queryParams]="{categoryId: child.id}" (click)="closeCategories()">{{ child.name }}<span aria-hidden="true">↗</span></a> }</div>
                      } @else { <p class="muted">Browse all products in {{ parent.name }}.</p> }
                    } @else { <p class="muted">Choose a category to explore its departments.</p> }
                  </div>
                </div>
              </div>
            }
          </div>
          <nav class="primary-nav" aria-label="Main navigation">
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{exact:true}" (click)="closeCategories()">Home</a>
            <a routerLink="/shop" routerLinkActive="active" (click)="closeCategories()">All products</a>
          </nav>
          <div class="nav-assurance"><span class="assurance-dot"></span>Freshness you can trust</div>
          @if (auth.hasRole('Admin') || auth.hasRole('Salesman')) { <a class="admin-shortcut" routerLink="/admin" aria-label="Open store management">Store management <span aria-hidden="true">↗</span></a> }
        </div>
      </div>
    </header>
    <main class="store-main"><router-outlet /></main>
    <footer class="store-footer">
      <div class="container footer-grid">
        <div class="footer-brand-block"><img class="footer-logo" src="/assets/smart-haat-brand.png" alt="Smart Haat"><p class="footer-copy">Everyday essentials and fresh choices, carefully selected for your home.</p></div>
        <div><h3>Explore Smart Haat</h3><a routerLink="/shop">All products</a><a routerLink="/cart">Your basket</a><a routerLink="/checkout">Checkout</a></div>
        <div><h3>Your account</h3><a routerLink="/login">Sign in</a><a routerLink="/register">Create account</a><a routerLink="/account/orders">Order history</a></div>
        <div><h3>Shopping with us</h3><p>Secure checkout</p><p>Cash on delivery</p><p>Prices in Bangladeshi taka</p></div>
      </div>
      <div class="container copyright">© 2026 Smart Haat <span>Freshness for everyday living</span></div>
    </footer>
  `
})
export class StoreLayout {
  private readonly api = inject(ApiService);
  readonly auth = inject(AuthService);
  readonly cart = inject(CartService);
  private readonly router = inject(Router);
  readonly categories = signal<Category[]>([]);
  readonly categoriesOpen = signal(false);
  readonly activeCategoryId = signal<string | null>(null);
  get rootCategories(): Category[] { return this.categories().filter(category => !category.parentCategoryId); }
  constructor() {
    void this.cart.refresh().catch(() => undefined);
    void firstValueFrom(this.api.get<Category[]>('/api/categories')).then(categories => {
      this.categories.set(categories);
      this.activeCategoryId.set(this.rootCategories[0]?.id ?? null);
    }).catch(() => undefined);
  }
  activeCategory(): Category | undefined { return this.categories().find(category => category.id === this.activeCategoryId()); }
  childrenOf(parentId: string): Category[] { return this.categories().filter(category => category.parentCategoryId === parentId); }
  toggleCategories(): void {
    const open = !this.categoriesOpen();
    this.categoriesOpen.set(open);
    if (open && !this.activeCategoryId()) this.activeCategoryId.set(this.rootCategories[0]?.id ?? null);
  }
  closeCategories(): void { this.categoriesOpen.set(false); }
  search(value: string): void { this.closeCategories(); void this.router.navigate(['/shop'], { queryParams: value.trim() ? { search: value.trim() } : {} }); }
}
