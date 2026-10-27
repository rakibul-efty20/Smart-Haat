import { CommonModule } from '@angular/common';
import { Component, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApiService } from '../core/api.service';
import { CartService } from '../core/cart.service';
import { Product } from '../core/models';

@Component({
  selector: 'app-product-card',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <article class="product-card">
      <a class="product-media" [routerLink]="['/products', product().id]">
        @if (product().imageUrl) { <img [src]="api.url(product().imageUrl!)" [alt]="product().name" loading="lazy"> }
        @else { <div class="product-placeholder" aria-hidden="true">{{ symbol }}</div> }
        @if (!product().isActive) { <span class="product-badge">Unavailable</span> }
      </a>
      <div class="product-body">
        <span class="product-category">{{ product().categoryName }} · {{ product().unit }}</span>
        <h3 class="product-name"><a [routerLink]="['/products', product().id]">{{ product().name }}</a></h3>
        @if (product().brand) { <span class="muted text-small">{{ product().brand }}</span> }
        <div class="product-footer">
          <div><span class="product-price">৳{{ product().price | number:'1.2-2' }}</span><span class="product-unit"> / {{ product().unit }}</span></div>
          <button class="product-add" type="button" [disabled]="!product().isActive || adding()" [attr.aria-label]="'Add ' + product().name + ' to cart'" (click)="add()">{{ adding() ? '…' : '+' }}</button>
        </div>
        @if (message()) { <small class="link-primary">{{ message() }}</small> }
      </div>
    </article>
  `
})
export class ProductCard {
  readonly product = input.required<Product>();
  readonly adding = signal(false);
  readonly message = signal('');
  constructor(readonly api: ApiService, private readonly cart: CartService) {}
  get symbol(): string { const name = this.product().categoryName.toLowerCase(); return name.includes('fruit') || name.includes('produce') ? '🍎' : name.includes('drink') || name.includes('beverage') ? '🥛' : name.includes('bak') ? '🥖' : name.includes('meat') || name.includes('fish') ? '🐟' : '🥬'; }
  async add(): Promise<void> { this.adding.set(true); this.message.set(''); try { await this.cart.add(this.product().id, 1); this.message.set('Added to cart'); } catch { this.message.set('Could not add. Check available stock.'); } finally { this.adding.set(false); } }
}
