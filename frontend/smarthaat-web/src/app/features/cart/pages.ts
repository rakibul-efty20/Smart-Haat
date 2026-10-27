import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';
import { Cart, CheckoutRequest, CreatedOrder, DeliveryZone, Order } from '../../core/models';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <header class="page-banner"><div class="container"><span class="eyebrow">Your basket</span><h1>Shopping cart</h1><p>Review your items before heading to checkout.</p></div></header>
    <section class="page-content container">
      @if (loading()) { <div class="loading"><span class="spinner"></span></div> }
      @else if (error()) { <div class="alert alert-error">{{ error() }}</div> }
      @else if (!cart.cart().items.length) { <div class="empty-state"><div class="empty-icon">🧺</div><h2>Your cart is ready for something fresh</h2><p>Browse the shop and add a few favourites. Your basket will stay with you while you shop.</p><a routerLink="/shop" class="button button-primary">Continue shopping</a></div> }
      @else {
        <div class="cart-layout"><section class="surface"><div class="row space-between"><h2>Your items <span class="muted">({{ cart.count() }})</span></h2><a routerLink="/shop" class="link-primary">Continue shopping</a></div>
          @for (item of cart.cart().items; track item.productId) {
            <div class="cart-row"><div class="cart-thumb">@if(item.imageUrl){<img [src]="api.url(item.imageUrl)" [alt]="item.name">}@else{<span>🥬</span>}</div><div><p class="cart-title"><a [routerLink]="['/products',item.productId]">{{ item.name }}</a></p><small class="muted">৳{{ item.unitPrice | number:'1.2-2' }} / {{ item.unit }} · {{ item.availableQuantity | number:'1.0-3' }} available</small><button type="button" class="link-primary text-small" style="display:block;border:0;background:none;padding:5px 0" (click)="remove(item.productId)">Remove</button></div><div class="quantity-control"><button type="button" aria-label="Decrease quantity" [disabled]="item.quantity<=0.001" (click)="set(item.productId,item.quantity-1)">−</button><input [attr.aria-label]="'Quantity of '+item.name" type="number" min="0.001" step="0.001" [value]="item.quantity" (change)="set(item.productId,+$any($event.target).value)"><button type="button" aria-label="Increase quantity" [disabled]="item.quantity>=item.availableQuantity" (click)="set(item.productId,item.quantity+1)">+</button></div><strong class="cart-line-total">৳{{ item.lineTotal | number:'1.2-2' }}</strong></div>
          }
          @if (notice()) { <div class="alert alert-error" style="margin-top:14px">{{ notice() }}</div> }
        </section>
        <aside class="surface summary-card"><h2>Order summary</h2><div class="summary-line"><span>Subtotal</span><strong>৳{{ cart.cart().subtotal | number:'1.2-2' }}</strong></div><div class="summary-line"><span>Delivery</span><span class="muted">Calculated at checkout</span></div><div class="summary-line total"><span>Estimated total</span><strong>৳{{ cart.cart().subtotal | number:'1.2-2' }}</strong></div><p class="muted text-small">Final delivery fee and any coupon discount are calculated by the store at checkout.</p><a routerLink="/checkout" class="button button-primary" style="width:100%">Continue to checkout →</a><div class="secure-note"><span>✓</span><span>Your order total is calculated securely by Smart Haat.</span></div></aside></div>
      }
    </section>
  `
})
export class CartPage {
  readonly cart = inject(CartService);
  readonly api = inject(ApiService);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly notice = signal('');
  constructor() { void this.cart.load().catch(() => this.error.set('Could not load your cart. Check that the API is running.')).finally(() => this.loading.set(false)); }
  async set(id: string, qty: number): Promise<void> { if (!Number.isFinite(qty) || qty <= 0) return; try { await this.cart.set(id, qty); this.notice.set(''); } catch { this.notice.set('Quantity could not be changed. Requested quantity may exceed available stock.'); await this.cart.refresh().catch(() => undefined); } }
  async remove(id: string): Promise<void> { try { await this.cart.remove(id); this.notice.set(''); } catch { this.notice.set('Could not remove this item. Try again.'); } }
}

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <header class="page-banner"><div class="container"><span class="eyebrow">Almost there</span><h1>Checkout</h1><p>Choose delivery details and review the server-calculated total.</p></div></header>
    <section class="page-content container">
      @if (loading()) { <div class="loading"><span class="spinner"></span></div> }
      @else if (!cart.cart().items.length) { <div class="empty-state"><h2>Your cart is empty</h2><p>Add products before placing an order.</p><a routerLink="/shop" class="button button-primary">Shop groceries</a></div> }
      @else {
        <div class="checkout-layout">
          <section class="surface"><h2>Delivery details</h2>
            @if (auth.user(); as user) { <div class="alert alert-info" style="margin-bottom:16px">Signed in as {{ user.displayName }}. Order history will be saved to your account.</div> }
            <form [formGroup]="form" (ngSubmit)="submit()" class="stack">
              <div class="form-grid">
                <div class="field"><label class="field-label" for="customer-name">Full name *</label><input id="customer-name" formControlName="customerName" autocomplete="name"><small class="form-error">{{ fieldError('customerName') }}</small></div>
                <div class="field"><label class="field-label" for="customer-phone">Phone number *</label><input id="customer-phone" formControlName="customerPhone" autocomplete="tel" placeholder="+8801..."><small class="form-error">{{ fieldError('customerPhone') }}</small></div>
                <div class="field wide"><label class="field-label" for="customer-email">Email (optional)</label><input id="customer-email" type="email" formControlName="customerEmail" autocomplete="email"><small class="form-error">{{ fieldError('customerEmail') }}</small></div>
                <div class="field wide"><label class="field-label" for="address-line">Delivery address *</label><textarea id="address-line" formControlName="addressLine" autocomplete="street-address" placeholder="House, road, area or a nearby landmark"></textarea><small class="form-error">{{ fieldError('addressLine') }}</small></div>
                <div class="field wide"><label class="field-label" for="delivery-zone">Delivery area *</label><select id="delivery-zone" formControlName="deliveryZoneCode"><option value="">Choose a delivery area</option>@for(zone of zones();track zone.id){<option [value]="zone.code">{{ zone.name }} · ৳{{ zone.fee | number:'1.2-2' }}</option>}</select><small class="form-error">{{ fieldError('deliveryZoneCode') }}</small></div>
                <div class="field wide"><label class="field-label" for="delivery-notes">Delivery notes (optional)</label><textarea id="delivery-notes" formControlName="deliveryNotes" placeholder="Gate, floor, or delivery instructions"></textarea></div>
                <div class="field wide"><label class="field-label" for="coupon-code">Coupon code (optional)</label><div class="row"><input id="coupon-code" formControlName="couponCode" placeholder="Enter a coupon"><button type="button" class="button button-accent" [disabled]="couponLoading()" (click)="validateCoupon()">{{ couponLoading() ? 'Checking...' : 'Apply coupon' }}</button></div>@if(couponMessage()){<small [class.form-error]="couponError()" [class.link-primary]="!couponError()">{{ couponMessage() }}</small>}</div>
              </div>
              @if (error()) { <div class="alert alert-error" role="alert">{{ error() }}</div> }
              <button type="submit" class="button button-primary" [disabled]="submitting() || form.invalid">{{ submitting() ? 'Placing your order...' : 'Place order · ৳' + total.toFixed(2) }}</button>
              <small class="muted">Payment method: Cash on delivery. Tax is not charged in this version.</small>
            </form>
          </section>
          <aside class="surface summary-card"><h2>Order summary</h2>@for(item of cart.cart().items;track item.productId){<div class="summary-line"><span>{{ item.name }} × {{ item.quantity }}</span><strong>৳{{ item.lineTotal | number:'1.2-2' }}</strong></div>}<div class="summary-line"><span>Subtotal</span><strong>৳{{ cart.cart().subtotal | number:'1.2-2' }}</strong></div><div class="summary-line"><span>Coupon</span><strong>{{ discount ? '−৳'+discount.toFixed(2) : '—' }}</strong></div><div class="summary-line"><span>Delivery</span><strong>{{ selectedZone ? ('৳'+selectedZone.fee.toFixed(2)) : 'Choose area' }}</strong></div><div class="summary-line total"><span>Total</span><strong>৳{{ total | number:'1.2-2' }}</strong></div><div class="secure-note"><span>✓</span><span>Smart Haat recalculates prices, coupons and stock before confirming.</span></div></aside>
        </div>
      }
    </section>
  `
})
export class CheckoutPage {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  readonly cart = inject(CartService);
  readonly auth = inject(AuthService);
  readonly zones = signal<DeliveryZone[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly couponLoading = signal(false);
  readonly couponMessage = signal('');
  readonly couponError = signal(false);
  readonly error = signal('');
  readonly quote = signal<{ discountAmount: number; code: string } | null>(null);
  readonly form = this.fb.group({ customerName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(160)]], customerPhone: ['', [Validators.required, Validators.pattern(/^[+()\-\s\d]{7,40}$/)]], customerEmail: ['', [Validators.email, Validators.maxLength(256)]], addressLine: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(400)]], deliveryZoneCode: ['', Validators.required], deliveryNotes: ['', Validators.maxLength(500)], couponCode: ['', Validators.maxLength(40)] });
  constructor() {
    const user = this.auth.user();
    if (user) this.form.patchValue({ customerName: user.displayName, customerEmail: user.email });
    Promise.all([this.cart.load(), firstValueFrom(this.api.get<DeliveryZone[]>('/api/delivery-zones'))]).then(([, zones]) => this.zones.set(zones)).catch(() => this.error.set('Could not load checkout data. Check that the API is running and a delivery zone is configured.')).finally(() => this.loading.set(false));
  }
  get selectedZone(): DeliveryZone | undefined { return this.zones().find(zone => zone.code === this.form.controls.deliveryZoneCode.value); }
  get discount(): number { return this.quote()?.discountAmount ?? 0; }
  get total(): number { return Math.max(0, this.cart.cart().subtotal - this.discount) + (this.selectedZone?.fee ?? 0); }
  fieldError(name: keyof typeof this.form.controls): string { const control = this.form.controls[name]; if (!control.touched || control.valid) return ''; return control.hasError('required') ? 'This field is required.' : 'Please check this value.'; }
  async validateCoupon(): Promise<void> {
    const code = this.form.controls.couponCode.value?.trim(); if (!code) { this.couponError.set(true); this.couponMessage.set('Enter a coupon code first.'); return; }
    this.couponLoading.set(true); this.couponMessage.set('');
    try { const quote = await firstValueFrom(this.api.post<{ code: string; discountAmount: number; payableBeforeDelivery: number }>('/api/coupons/validate', { code })); this.quote.set({ code: quote.code, discountAmount: quote.discountAmount }); this.couponError.set(false); this.couponMessage.set(`Coupon applied. You save ৳${quote.discountAmount.toFixed(2)}.`); }
    catch { this.quote.set(null); this.couponError.set(true); this.couponMessage.set('Coupon could not be applied to this cart. Check the code and eligibility.'); }
    finally { this.couponLoading.set(false); }
  }
  async submit(): Promise<void> {
    this.form.markAllAsTouched(); if (this.form.invalid || !this.selectedZone) return;
    this.submitting.set(true); this.error.set('');
    const value = this.form.getRawValue();
    const request: CheckoutRequest = { customerName: value.customerName!, customerPhone: value.customerPhone!, customerEmail: value.customerEmail || null, addressLine: value.addressLine!, deliveryZoneCode: value.deliveryZoneCode!, deliveryNotes: value.deliveryNotes || null, couponCode: value.couponCode?.trim() || null };
    try {
      const result = await firstValueFrom(this.api.post<CreatedOrder>('/api/orders', request, this.auth.signedIn ? { 'Idempotency-Key': `web-${crypto.randomUUID()}` } : undefined));
      sessionStorage.setItem('smarthaat.last-order', JSON.stringify(result.order));
      await this.cart.refresh().catch(() => undefined);
      await this.router.navigate(['/checkout/confirmation']);
    } catch (error: any) { this.error.set(error?.error?.detail || error?.error?.message || 'The order could not be placed. Check that your cart, delivery area and available stock are valid.'); }
    finally { this.submitting.set(false); }
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page-content container"><div class="surface checkout-success">
      <div class="success-mark">✓</div><span class="eyebrow">Order received</span><h1>Thank you for shopping with us</h1>
      @if(order(); as value){<p>Your order <strong>{{ value.orderNumber }}</strong> was placed successfully. It is now <span class="status-badge status-pending">{{ statusName(value.status) }}</span>.</p><div class="admin-card" style="max-width:480px;margin:24px auto;text-align:left"><div class="summary-line"><span>Items</span><strong>{{ value.items.length }}</strong></div><div class="summary-line"><span>Payment</span><strong>Cash on delivery</strong></div><div class="summary-line total"><span>Order total</span><strong>৳{{ value.total | number:'1.2-2' }}</strong></div></div><div class="row" style="justify-content:center;margin-top:22px"><a routerLink="/shop" class="button button-primary">Continue shopping</a>@if(auth.signedIn){<a routerLink="/account/orders" class="button button-outline">View order history</a>}</div>}@else{<p>Order details are not available in this browser session.</p><a routerLink="/shop" class="button button-primary">Continue shopping</a>}
    </div></section>
  `
})
export class OrderConfirmationPage {
  readonly order = signal<Order | null>(null);
  readonly auth = inject(AuthService);
  constructor() { try { this.order.set(JSON.parse(sessionStorage.getItem('smarthaat.last-order') ?? 'null') as Order | null); } catch { /* ignore invalid session state */ } }
  statusName(value: number): string { return ({ 1:'Pending',2:'Confirmed',3:'Packed',4:'Out for delivery',5:'Delivered',6:'Completed',7:'Cancelled' } as Record<number,string>)[value] ?? 'Pending'; }
}


