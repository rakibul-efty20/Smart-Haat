import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { CartService } from '../../core/cart.service';
import { Category, Page, Product } from '../../core/models';
import { ProductCard } from '../../shared/product-card';

@Component({
  standalone: true,
  imports: [CommonModule, ProductCard],
  template: `
    <header class="page-banner"><div class="container"><span class="eyebrow">The Smart Haat shop</span><h1>Everyday groceries</h1><p>Search the shelves, compare prices, and add your favourites to cart.</p></div></header>
    <section class="page-content container">
      <div class="catalog-toolbar"><div class="catalog-result-label"><strong>Find your favourites</strong><span class="muted"> Browse by department or refine your search</span></div>
        <label class="sr-only" for="sort-products">Sort products</label><select id="sort-products" class="sort-select" [value]="sort()" (change)="changeSort($any($event.target).value)"><option value="name_asc">Name: A to Z</option><option value="name_desc">Name: Z to A</option><option value="price_asc">Price: low to high</option><option value="price_desc">Price: high to low</option></select></div>
      <div class="catalog-layout">
        <aside class="filter-panel"><span class="eyebrow">SHOP DEPARTMENTS</span><h3>Find what you need</h3><div class="shop-category-tree"><button type="button" class="shop-category-all" [class.selected]="!categoryId()" (click)="setCategory(null)">All products</button>@for(parent of rootCategories;track parent.id){<div class="shop-category-group"><div class="shop-category-parent"><button type="button" [class.selected]="categoryId()===parent.id" (click)="setCategory(parent.id)">{{ parent.name }}</button>@if(childrenOf(parent.id).length){<button type="button" class="tree-toggle" [attr.aria-label]="(expandedCategories().has(parent.id)?'Collapse ':'Expand ')+parent.name" [attr.aria-expanded]="expandedCategories().has(parent.id)" (click)="toggleCategory(parent.id)"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="m9 18 6-6-6-6"></path></svg></button>}</div>@if(expandedCategories().has(parent.id)){<div class="shop-category-children">@for(child of childrenOf(parent.id);track child.id){<button type="button" [class.selected]="categoryId()===child.id" (click)="setCategory(child.id)">{{ child.name }}</button>}</div>}</div>}</div><div class="filter-divider"></div><label for="search-products">Product name or brand</label><input id="search-products" type="search" [value]="search()" (input)="search.set($any($event.target).value)" (keydown.enter)="applyFilters()" placeholder="e.g. mango"><div class="form-grid" style="margin-top:16px"><div class="field"><label class="field-label" for="min-price">Min price</label><input id="min-price" type="number" min="0" [value]="minPrice()" (input)="minPrice.set($any($event.target).value)"></div><div class="field"><label class="field-label" for="max-price">Max price</label><input id="max-price" type="number" min="0" [value]="maxPrice()" (input)="maxPrice.set($any($event.target).value)"></div></div><button class="button button-primary" style="width:100%;margin-top:14px" (click)="applyFilters()">Apply filters</button><button class="button button-outline" style="width:100%;margin-top:8px" (click)="clearFilters()">Clear filters</button></aside>
        <div class="catalog-content">
          @if (error()) { <div class="alert alert-error">{{ error() }}</div> }
          @if (loading()) { <div class="loading"><span class="spinner" aria-label="Loading products"></span></div> }
          @else if (page()?.items?.length) { <div class="section-head"><div><strong>{{ page()?.totalCount }} products</strong><span class="muted"> matching your search</span></div></div><div class="product-grid">@for (product of page()!.items; track product.id) { <app-product-card [product]="product" /> }</div>
            <div class="pagination"><span>Page {{ page()?.pageNumber }} of {{ totalPages }}</span><div class="pagination-actions"><button class="button button-outline button-sm" [disabled]="currentPage()<=1" (click)="goPage(currentPage()-1)">Previous</button><button class="button button-outline button-sm" [disabled]="currentPage()>=totalPages" (click)="goPage(currentPage()+1)">Next</button></div></div>
          } @else { <div class="empty-state"><div class="empty-icon">🔎</div><h3>No matching products</h3><p>Try another search or clear your filters. Active, in-stock products appear here.</p><button class="button button-outline" (click)="clearFilters()">Clear filters</button></div> }
        </div>
      </div>
    </section>
  `
})
export class ProductsPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  readonly page = signal<Page<Product> | null>(null);
  readonly categories = signal<Category[]>([]);
  readonly expandedCategories = signal<Set<string>>(new Set());
  readonly loading = signal(true);
  readonly error = signal('');
  readonly search = signal('');
  readonly categoryId = signal<string | null>(null);
  readonly minPrice = signal('');
  readonly maxPrice = signal('');
  readonly sort = signal('name_asc');
  readonly currentPage = signal(1);
  get rootCategories(): Category[] { return this.categories().filter(category => !category.parentCategoryId); }
  get totalPages(): number { return Math.max(1, Math.ceil((this.page()?.totalCount ?? 0) / (this.page()?.pageSize ?? 12))); }
  constructor() {
    void firstValueFrom(this.api.get<Category[]>('/api/categories')).then(v => this.categories.set(v)).catch(() => undefined);
    this.route.queryParamMap.subscribe(params => { this.search.set(params.get('search') ?? ''); this.categoryId.set(params.get('categoryId')); this.currentPage.set(1); void this.load(); });
  }
  async load(): Promise<void> { this.loading.set(true); this.error.set(''); try { this.page.set(await firstValueFrom(this.api.get<Page<Product>>('/api/products', { search: this.search(), categoryId: this.categoryId(), minPrice: this.minPrice(), maxPrice: this.maxPrice(), sort: this.sort(), page: this.currentPage(), pageSize: 12 }))); } catch { this.error.set('We could not load products. Check that the API is running, then try again.'); } finally { this.loading.set(false); } }
  setCategory(value: string | null): void { this.categoryId.set(value); this.currentPage.set(1); void this.load(); }
  childrenOf(parentId: string): Category[] { return this.categories().filter(category => category.parentCategoryId === parentId); }
  toggleCategory(id: string): void { const expanded = new Set(this.expandedCategories()); expanded.has(id) ? expanded.delete(id) : expanded.add(id); this.expandedCategories.set(expanded); }
  changeSort(value: string): void { this.sort.set(value); this.currentPage.set(1); void this.load(); }
  applyFilters(): void { this.currentPage.set(1); void this.load(); }
  clearFilters(): void { this.search.set(''); this.categoryId.set(null); this.minPrice.set(''); this.maxPrice.set(''); this.currentPage.set(1); void this.load(); }
  goPage(page: number): void { this.currentPage.set(page); void this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }
}

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page-content container">
      <div class="breadcrumb"><a routerLink="/shop" class="link-primary">Shop</a> / {{ product()?.name ?? 'Product' }}</div>
      @if (loading()) { <div class="loading"><span class="spinner"></span></div> }
      @else if (error()) { <div class="empty-state"><h2>Product unavailable</h2><p>{{ error() }}</p><a routerLink="/shop" class="button button-outline">Back to shop</a></div> }
      @else if (product(); as item) {
        <div class="detail-layout"><div class="detail-image">@if(item.imageUrl){<img [src]="api.url(item.imageUrl)" [alt]="item.name">}@else{<div class="product-placeholder">🥬</div>}</div>
          <div><span class="eyebrow">{{ item.categoryName }} · {{ item.unit }}</span><h1 style="margin:12px 0">{{ item.name }}</h1>@if(item.brand){<p class="muted">{{ item.brand }}</p>}<div class="detail-price">৳{{ item.price | number:'1.2-2' }} <span class="product-unit">/ {{ item.unit }}</span></div><p>{{ item.description || 'A carefully selected everyday essential from Smart Haat.' }}</p><p class="alert alert-info">Available stock is checked by the store when you add this item and again at checkout.</p><div class="row" style="margin-top:20px"><div class="quantity-control"><button aria-label="Decrease quantity" (click)="decrement()">−</button><input aria-label="Quantity" type="number" min="1" [value]="quantity()" (input)="quantity.set(+$any($event.target).value)"><button aria-label="Increase quantity" (click)="increment()">+</button></div><button class="button button-primary" [disabled]="adding()" (click)="add()">{{ adding() ? 'Adding...' : 'Add to cart' }}</button></div>@if(message()){<p class="alert alert-success" style="margin-top:15px">{{ message() }} <a routerLink="/cart" class="link-primary">View cart</a></p>}</div>
        </div>
      }
    </section>
  `
})
export class ProductPage {
  private readonly route = inject(ActivatedRoute);
  private readonly cart = inject(CartService);
  readonly api = inject(ApiService);
  readonly product = signal<Product | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly quantity = signal(1);
  readonly adding = signal(false);
  readonly message = signal('');
  decrement(): void { this.quantity.update(value => Math.max(1, value - 1)); }
  increment(): void { this.quantity.update(value => value + 1); }
  constructor() { this.route.paramMap.subscribe(params => { const id = params.get('id'); if (id) void this.load(id); }); }
  async load(id: string): Promise<void> { this.loading.set(true); try { this.product.set(await firstValueFrom(this.api.get<Product>(`/api/products/${id}`))); } catch { this.error.set('This product is missing, inactive, or the store could not be reached.'); } finally { this.loading.set(false); } }
  async add(): Promise<void> { const product = this.product(); if (!product) return; this.adding.set(true); try { await this.cart.add(product.id, Math.max(1, this.quantity())); this.message.set('Added to your cart.'); } catch { this.message.set('Could not add this amount. Check the available stock.'); } finally { this.adding.set(false); } }
}

