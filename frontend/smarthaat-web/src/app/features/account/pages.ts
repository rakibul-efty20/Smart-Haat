import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthProfileError, AuthService } from '../../core/auth.service';
import { CartService } from '../../core/cart.service';
import { Order, Page } from '../../core/models';

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page"><section class="auth-art"><img src="/assets/smart-haat-brand.png" alt="Smart Haat"><span class="eyebrow" style="color:#86efac">{{ isAdminLogin ? 'Staff access' : 'Welcome back' }}</span><h1>{{ isAdminLogin ? 'Manage your store with confidence.' : 'Your everyday shop is just a sign in away.' }}</h1><p>Fresh finds, clear order updates, and an easy way to shop the things you need.</p></section>
      <section class="auth-panel"><div class="auth-card">@if(isAdminLogin){<span class="staff-banner">ADMIN AND STAFF PORTAL</span>}<span class="eyebrow">{{ isAdminLogin ? 'Smart Haat workspace' : 'Smart Haat account' }}</span><h1>Sign in</h1><p class="muted">{{ isAdminLogin ? 'Use your provisioned Admin or Salesman account.' : 'Welcome back. Enter your account details below.' }}</p>
        <form [formGroup]="form" (ngSubmit)="submit()"><div class="field"><label class="field-label" for="email">Email address</label><input id="email" type="email" formControlName="email" autocomplete="username email"><small class="form-error">{{ fieldError('email') }}</small></div><div class="field"><label class="field-label" for="password">Password</label><input id="password" type="password" formControlName="password" autocomplete="current-password"><small class="form-error">{{ fieldError('password') }}</small></div>@if(error()){<div class="alert alert-error" role="alert">{{ error() }}</div>}<button class="button button-primary" type="submit" [disabled]="loading() || form.invalid">{{ loading() ? 'Signing in...' : 'Sign in' }}</button></form>
        <p class="auth-foot">{{ isAdminLogin ? 'Need the storefront?' : 'New to Smart Haat?' }} @if(isAdminLogin){<a routerLink="/">Back to shop</a>}@else{<a routerLink="/register">Create a customer account</a>}</p>@if(!isAdminLogin){<p class="auth-foot"><a routerLink="/admin/login">Staff and Admin sign in →</a></p>}
      </div></section></div>
  `
})
export class LoginPage {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  readonly form = this.fb.group({ email: [this.route.snapshot.queryParamMap.get('email') ?? '', [Validators.required, Validators.email]], password: ['', Validators.required] });
  readonly loading = signal(false);
  readonly error = signal('');
  get isAdminLogin(): boolean { return this.router.url.startsWith('/admin/login'); }
  fieldError(name: 'email' | 'password'): string { const c=this.form.controls[name]; return c.touched && c.invalid ? (c.hasError('required') ? 'This field is required.' : 'Enter a valid email address.') : ''; }
  async submit(): Promise<void> {
    this.form.markAllAsTouched(); if (this.form.invalid) return;
    this.loading.set(true); this.error.set('');
    try {
      const value=this.form.getRawValue(); const user=await this.auth.login(value.email!, value.password!);
      if (this.isAdminLogin && !user.roles.some(role => role === 'Admin' || role === 'Salesman')) { await this.auth.logout().catch(() => undefined); this.error.set('This account does not have staff or Admin access.'); return; }
      if (user.roles.includes('Customer')) await this.cart.merge().catch(() => undefined);
      const requested=this.route.snapshot.queryParamMap.get('returnUrl');
      const destination=requested?.startsWith('/') ? requested : user.roles.includes('Admin') ? '/admin/dashboard' : user.roles.includes('Salesman') ? '/admin/pos' : '/account/orders';
      await this.router.navigateByUrl(destination);
    } catch (error: any) {
      const status = error?.status;
      if (error instanceof AuthProfileError) this.error.set('Sign-in was accepted, but the portal could not load your account profile. Restart the API with the latest backend build and try again.');
      else if (status === 401) this.error.set('The API rejected these credentials. Check the email and password, then try again.');
      else if (status === 0) this.error.set('Cannot reach the API. Start SmartHaat.Api and check its HTTPS certificate and AllowedOrigins settings.');
      else if (status === 429) this.error.set('Too many login attempts. Wait a minute before trying again.');
      else this.error.set(error?.error?.detail || error?.error?.message || error?.error?.title || 'Sign in failed. Check the API response and try again.');
    }
    finally { this.loading.set(false); }
  }
}

@Component({
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-page"><section class="auth-art"><img src="/assets/smart-haat-brand.png" alt="Smart Haat"><span class="eyebrow" style="color:#86efac">A simpler grocery run</span><h1>Good things start at home.</h1><p>Create a Smart Haat customer account to keep your orders together and make your next shop easier.</p></section><section class="auth-panel"><div class="auth-card"><span class="eyebrow">Join Smart Haat</span><h1>Create an account</h1><p class="muted">All fields are required unless marked optional.</p><form [formGroup]="form" (ngSubmit)="submit()"><div class="field"><label class="field-label" for="display-name">Full name</label><input id="display-name" formControlName="displayName" autocomplete="name"><small class="form-error">{{ fieldError('displayName') }}</small></div><div class="field"><label class="field-label" for="register-email">Email address</label><input id="register-email" type="email" formControlName="email" autocomplete="email"><small class="form-error">{{ fieldError('email') }}</small></div><div class="field"><label class="field-label" for="register-password">Password</label><input id="register-password" type="password" formControlName="password" autocomplete="new-password"><small class="form-error">{{ fieldError('password') }}</small></div><div class="field"><label class="field-label" for="confirm-password">Confirm password</label><input id="confirm-password" type="password" formControlName="confirmPassword" autocomplete="new-password"><small class="form-error">{{ fieldError('confirmPassword') }}</small></div>@if(error()){<div class="alert alert-error">{{ error() }}</div>}@if(success()){<div class="alert alert-success">Account created. You can now sign in.</div>}<button class="button button-primary" type="submit" [disabled]="loading() || form.invalid">{{ loading() ? 'Creating account...' : 'Create customer account' }}</button></form><p class="auth-foot">Already registered? <a routerLink="/login">Sign in</a></p></div></section></div>
  `
})
export class RegisterPage {
  private readonly fb=inject(FormBuilder);
  private readonly router=inject(Router);
  private readonly auth=inject(AuthService);
  readonly form=this.fb.group({displayName:['',[Validators.required,Validators.minLength(2),Validators.maxLength(160)]],email:['',[Validators.required,Validators.email,Validators.maxLength(256)]],password:['',[Validators.required,Validators.minLength(10)]],confirmPassword:['',[Validators.required]]});
  readonly loading=signal(false);
  readonly error=signal('');
  readonly success=signal(false);
  fieldError(name:'displayName'|'email'|'password'|'confirmPassword'):string { const c=this.form.controls[name]; if(!c.touched)return ''; if(c.hasError('required'))return 'This field is required.'; if(c.hasError('email'))return 'Enter a valid email.'; if(c.hasError('minlength'))return name==='password'?'Use at least 10 characters.':'Enter at least 2 characters.'; if(name==='confirmPassword' && c.value!==this.form.controls.password.value)return 'Passwords do not match.'; return ''; }
  async submit():Promise<void>{this.form.markAllAsTouched();if(this.form.invalid||this.form.controls.password.value!==this.form.controls.confirmPassword.value)return;this.loading.set(true);this.error.set('');try{const v=this.form.getRawValue();await this.auth.register(v.displayName!,v.email!,v.password!);this.success.set(true);await this.router.navigate(['/login'],{queryParams:{email:v.email}});}catch(error:any){const errors=error?.error?.errors;this.error.set(Array.isArray(errors)?errors.map((e:any)=>e.description??e).join(' '):'Account could not be created. This email may already be registered or the password does not meet the account policy.');}finally{this.loading.set(false);}}
}

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <header class="page-banner"><div class="container"><span class="eyebrow">Your account</span><h1>Order history</h1><p>Orders placed while signed in to this account.</p></div></header><section class="page-content container">
      @if(loading()){<div class="loading"><span class="spinner"></span></div>}@else if(error()){<div class="alert alert-error">{{ error() }}</div>}@else if(!page()?.items?.length){<div class="empty-state"><div class="empty-icon">📦</div><h2>No account orders yet</h2><p>Orders placed as a guest are not attached to your account. Start a new order while signed in to see it here.</p><a routerLink="/shop" class="button button-primary">Start shopping</a></div>}@else{<div class="surface"><div class="table-wrap"><table><thead><tr><th>Order</th><th>Placed</th><th>Items</th><th>Status</th><th>Payment</th><th>Total</th><th></th></tr></thead><tbody>@for(order of page()!.items;track order.id){<tr><td><strong>{{ order.orderNumber }}</strong></td><td>{{ order.createdAtUtc | date:'mediumDate' }}</td><td>{{ order.items.length }}</td><td><span class="status-badge" [class.status-pending]="order.status===1" [class.status-confirmed]="order.status===2" [class.status-packed]="order.status===3" [class.status-delivered]="order.status===5" [class.status-cancelled]="order.status===7">{{ statusName(order.status) }}</span></td><td>{{ order.paymentStatus===2?'Paid':'Cash on delivery' }}</td><td><strong>৳{{ order.total | number:'1.2-2' }}</strong></td><td><a class="link-primary" [routerLink]="['/account/orders',order.id]">Details →</a></td></tr>}</tbody></table></div><div class="pagination"><span>{{ page()?.totalCount }} orders</span><div class="pagination-actions"><button class="button button-outline button-sm" [disabled]="pageNumber()<=1" (click)="load(pageNumber()-1)">Previous</button><span>Page {{ pageNumber() }}</span><button class="button button-outline button-sm" [disabled]="pageNumber()*20>=(page()?.totalCount??0)" (click)="load(pageNumber()+1)">Next</button></div></div></div>}
    </section>
  `
})
export class AccountOrdersPage {
  private readonly api=inject(ApiService);
  readonly page=signal<Page<Order>|null>(null);
  readonly pageNumber=signal(1);
  readonly loading=signal(true);
  readonly error=signal('');
  constructor(){void this.load(1);}
  async load(page:number):Promise<void>{this.loading.set(true);this.error.set('');try{this.page.set(await firstValueFrom(this.api.get<Page<Order>>('/api/orders/my',{page,pageSize:20})));this.pageNumber.set(page);}catch{this.error.set('Could not load order history. Sign in again and try once more.');}finally{this.loading.set(false);}}
  statusName(v:number):string{return({1:'Pending',2:'Confirmed',3:'Packed',4:'Out for delivery',5:'Delivered',6:'Completed',7:'Cancelled'}as Record<number,string>)[v]??'Unknown';}
}

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page-content container">@if(loading()){<div class="loading"><span class="spinner"></span></div>}@else if(error()){<div class="empty-state"><h2>Order not found</h2><p>{{ error() }}</p><a routerLink="/account/orders" class="button button-outline">Back to orders</a></div>}@else if(order();as item){<div class="breadcrumb"><a routerLink="/account/orders" class="link-primary">Order history</a> / {{ item.orderNumber }}</div><div class="admin-page-head"><div><span class="eyebrow">Order details</span><h2>{{ item.orderNumber }}</h2><p>Placed {{ item.createdAtUtc | date:'medium' }}</p></div><span class="status-badge" [class.status-pending]="item.status===1" [class.status-delivered]="item.status===5">{{ statusName(item.status) }}</span></div><div class="cart-layout"><div class="surface"><h2>Items</h2>@for(line of item.items;track line.sku){<div class="cart-row"><div class="cart-thumb"><span>🥬</span></div><div><p class="cart-title">{{ line.name }}</p><small class="muted">{{ line.sku }} · ৳{{ line.unitPrice | number:'1.2-2' }} / {{ line.unit }}</small></div><span>× {{ line.quantity }}</span><strong class="cart-line-total">৳{{ line.lineTotal | number:'1.2-2' }}</strong></div>}</div><aside class="surface"><h2>Delivery & payment</h2><p><strong>{{ item.customerName }}</strong><br>{{ item.customerPhone }}<br>{{ item.addressLine }}<br>{{ item.area }}</p><div class="summary-line"><span>Payment</span><strong>{{ item.paymentStatus===2?'Paid':'Cash on delivery' }}</strong></div><div class="summary-line"><span>Subtotal</span><span>৳{{ item.subtotal | number:'1.2-2' }}</span></div><div class="summary-line"><span>Discount</span><span>−৳{{ item.discountTotal | number:'1.2-2' }}</span></div><div class="summary-line"><span>Delivery</span><span>৳{{ item.deliveryFee | number:'1.2-2' }}</span></div><div class="summary-line total"><span>Total</span><strong>৳{{ item.total | number:'1.2-2' }}</strong></div></aside></div>}</section>
  `
})
export class AccountOrderDetailPage {
  private readonly api=inject(ApiService);
  private readonly route=inject(ActivatedRoute);
  readonly order=signal<Order|null>(null);
  readonly loading=signal(true);
  readonly error=signal('');
  constructor(){const id=this.route.snapshot.paramMap.get('id');if(!id){this.error.set('Missing order ID.');this.loading.set(false);}else void firstValueFrom(this.api.get<Order>(`/api/orders/${id}`)).then(value=>this.order.set(value)).catch(()=>this.error.set('This order is unavailable or does not belong to your account.')).finally(()=>this.loading.set(false));}
  statusName(v:number):string{return({1:'Pending',2:'Confirmed',3:'Packed',4:'Out for delivery',5:'Delivered',6:'Completed',7:'Cancelled'}as Record<number,string>)[v]??'Unknown';}
}

