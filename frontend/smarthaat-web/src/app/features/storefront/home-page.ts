import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Category, Page, Product } from '../../core/models';
import { ProductCard } from '../../shared/product-card';

type CategoryShelf = { category: Category; products: Product[]; theme: number };

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink, ProductCard],
  template: `
    <section class="home-promotions container" aria-label="Smart Haat offers and services">
      <a class="promotion-card promotion-fresh" routerLink="/shop">
        <span class="promotion-kicker">SMART HAAT · FRESH DAILY</span><h1>Fresh from the market.<br><em>Ready for your table.</em></h1>
        <span class="promotion-link">Shop fresh groceries <b aria-hidden="true">→</b></span>
        <span class="promotion-art produce-art" aria-hidden="true"><i>🥬</i><i>🍅</i><i>🥕</i></span>
      </a>
      <a class="promotion-card promotion-delivery" routerLink="/shop">
        <span class="promotion-kicker">YOUR NEIGHBOURHOOD SHOP</span><h2>Your everyday basket,<br>delivered with care.</h2><span class="promotion-link">Explore the shop <b aria-hidden="true">→</b></span>
        <span class="delivery-art" aria-hidden="true"><span class="delivery-sun"></span><span class="delivery-route">⌁</span><span class="delivery-pin">⌖</span></span>
      </a>
      <a class="promotion-card promotion-value" routerLink="/shop">
        <span class="promotion-kicker">SMART SAVINGS, EVERY DAY</span><h2>Good choices.<br><em>Good value.</em></h2><span class="promotion-link">Find your favourites <b aria-hidden="true">→</b></span>
        <span class="value-art" aria-hidden="true"><i>৳</i><i>৳</i><i>৳</i></span>
      </a>
      <a class="promotion-card promotion-home" routerLink="/shop">
        <span class="promotion-kicker">A LITTLE MORE CONVENIENT</span><h2>Stock up on<br>the things you love.</h2><span class="promotion-link">Start shopping <b aria-hidden="true">→</b></span>
        <span class="home-art" aria-hidden="true"><i>🛍️</i><i>🍊</i><i>🥛</i></span>
      </a>
    </section>

    <section class="service-highlights"><div class="container service-highlight-grid">
      <div><span class="service-icon"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6l-7-3Z"></path><path d="m9 12 2 2 4-4"></path></svg></span><span><strong>Shop with confidence</strong><small>Clear product and pricing information</small></span></div>
      <div><span class="service-icon"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 7h11v10H3zM14 10h4l3 3v4h-7z"></path><circle cx="7.5" cy="19" r="1.5"></circle><circle cx="18" cy="19" r="1.5"></circle></svg></span><span><strong>Convenient delivery</strong><small>Choose your delivery area at checkout</small></span></div>
      <div><span class="service-icon"><svg viewBox="0 0 24 24" aria-hidden="true"><rect x="4" y="6" width="16" height="13" rx="2"></rect><path d="M8 6V4h8v2M8 12h8"></path></svg></span><span><strong>Cash on delivery</strong><small>Pay when your order arrives</small></span></div>
    </div></section>

    <section class="section container home-category-section">
      <div class="section-head"><div><span class="eyebrow">Shop the way you like</span><h2>Browse our departments</h2><p>Choose a department, then explore its categories.</p></div><a routerLink="/shop" class="link-primary">All products <span aria-hidden="true">→</span></a></div>
      @if (loading()) { <div class="loading"><span class="spinner" aria-label="Loading categories"></span></div> }
      @else if (rootCategories.length) {
        <div class="department-grid">@for (category of rootCategories; track category.id) {
          <article class="department-card"><a class="department-main" [routerLink]="['/shop']" [queryParams]="{categoryId: category.id}"><span class="department-icon" aria-hidden="true">{{ categoryIcon(category.name) }}</span><span><small>DEPARTMENT</small><strong>{{ category.name }}</strong></span><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 18 6-6-6-6"></path></svg></a>
            @if (childrenOf(category.id).length) { <div class="department-subcategories">@for(child of childrenOf(category.id).slice(0,4);track child.id){<a [routerLink]="['/shop']" [queryParams]="{categoryId: child.id}">{{ child.name }}<span aria-hidden="true">↗</span></a>}</div> }
          </article>
        }</div>
      } @else { <div class="empty-state"><h3>Departments coming soon</h3><p>Product categories will appear here as the catalog is set up.</p><a routerLink="/shop" class="button button-outline">Browse products</a></div> }
    </section>

    <section class="section container shelf-section">
      <div class="section-head"><div><span class="eyebrow">A good place to start</span><h2>Popular picks</h2><p>Everyday essentials shoppers come back for.</p></div><a routerLink="/shop" class="link-primary">See all products <span aria-hidden="true">→</span></a></div>
      @if (productsLoading()) { <div class="loading"><span class="spinner" aria-label="Loading popular products"></span></div> }
      @else if (popularProducts().length) {
        <div class="shelf-row"><aside class="shelf-promo shelf-promo-popular"><span class="shelf-promo-kicker">SMART HAAT FAVOURITES</span><span class="shelf-promo-art" aria-hidden="true">🥬</span><h3>Good things<br>for every day.</h3><p>Fresh finds and pantry staples for your home.</p><a routerLink="/shop">Explore the shop <span aria-hidden="true">→</span></a></aside>
          <div class="shelf-track">@for(product of popularProducts();track product.id){<app-product-card [product]="product" />}</div>
        </div>
      } @else { <div class="empty-state"><h3>Your shelves are getting ready</h3><p>Once the Admin adds active products and receives stock, shoppers can browse them here.</p></div> }
    </section>

    @for (shelf of shelves(); track shelf.category.id; let index = $index) {
      <section class="section container category-shelf-section">
        <div class="section-head"><div><span class="eyebrow">Selected for you</span><h2>{{ shelf.category.name }}</h2><p>Shop the latest in {{ shelf.category.name.toLowerCase() }}.</p></div><a [routerLink]="['/shop']" [queryParams]="{categoryId:shelf.category.id}" class="link-primary">Shop {{ shelf.category.name }} <span aria-hidden="true">→</span></a></div>
        <div class="shelf-row"><aside class="shelf-promo" [class]="'shelf-promo shelf-promo-theme-' + shelf.theme"><span class="shelf-promo-kicker">SMART HAAT DEPARTMENT</span><span class="shelf-promo-art" aria-hidden="true">{{ categoryIcon(shelf.category.name) }}</span><h3>{{ shelf.category.name }}<br>for your home.</h3><p>Thoughtful everyday choices, all in one place.</p><a [routerLink]="['/shop']" [queryParams]="{categoryId:shelf.category.id}">Browse category <span aria-hidden="true">→</span></a></aside>
          <div class="shelf-track">@for(product of shelf.products;track product.id){<app-product-card [product]="product" />}</div>
        </div>
      </section>
    }

    <section class="container home-bottom-cta"><div><span class="eyebrow">Your next grocery run</span><h2>Make room for the good stuff.</h2><p>Explore the Smart Haat range and build a basket that works for your home.</p></div><a routerLink="/shop" class="button button-light">Start shopping <span aria-hidden="true">→</span></a></section>
  `
})
export class HomePage {
  private readonly api = inject(ApiService);
  readonly categories = signal<Category[]>([]);
  readonly popularProducts = signal<Product[]>([]);
  readonly shelves = signal<CategoryShelf[]>([]);
  readonly loading = signal(true);
  readonly productsLoading = signal(true);
  get rootCategories(): Category[] { return this.categories().filter(category => !category.parentCategoryId); }
  constructor() { void this.initialize(); }
  private async initialize(): Promise<void> {
    const categoryTask = firstValueFrom(this.api.get<Category[]>('/api/categories')).then(value => this.categories.set(value)).catch(() => undefined).finally(() => this.loading.set(false));
    const productTask = firstValueFrom(this.api.get<Page<Product>>('/api/products', { page: 1, pageSize: 8, sort: 'name_asc' })).then(value => this.popularProducts.set(value.items)).catch(() => undefined).finally(() => this.productsLoading.set(false));
    await Promise.all([categoryTask, productTask]);
    const parents = this.rootCategories.slice(0, 4);
    const shelves = await Promise.all(parents.map(async (category, theme) => {
      const categoryIds = [category.id, ...this.childrenOf(category.id).map(child => child.id)];
      const pages = await Promise.all(categoryIds.map(id => firstValueFrom(this.api.get<Page<Product>>('/api/products', { categoryId: id, page: 1, pageSize: 6, sort: 'name_asc' })).catch(() => null)));
      const products = pages.flatMap(page => page?.items ?? []).slice(0, 5);
      return products.length ? { category, products, theme: theme % 4 } : null;
    }));
    this.shelves.set(shelves.filter((shelf): shelf is CategoryShelf => shelf !== null));
  }
  childrenOf(parentId: string): Category[] { return this.categories().filter(category => category.parentCategoryId === parentId); }
  categoryIcon(name: string): string {
    const value = name.toLowerCase();
    return value.includes('fruit') || value.includes('produce') ? '🍎' : value.includes('dairy') ? '🥛' : value.includes('bak') ? '🥖' : value.includes('drink') || value.includes('beverage') ? '🧃' : value.includes('meat') || value.includes('fish') ? '🥩' : value.includes('clean') || value.includes('house') ? '🧴' : '🥬';
  }
}
