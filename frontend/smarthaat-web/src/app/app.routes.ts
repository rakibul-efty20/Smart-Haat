import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/guards';
import { StoreLayout } from './layouts/store-layout';
import { AdminLayout } from './layouts/admin-layout';

export const routes: Routes = [
  { path: 'admin/login', loadComponent: () => import('./features/account/pages').then(m => m.LoginPage), data: { adminLogin: true } },
  {
    path: '', component: StoreLayout, children: [
      { path: '', pathMatch: 'full', loadComponent: () => import('./features/storefront/home-page').then(m => m.HomePage) },
      { path: 'shop', loadComponent: () => import('./features/storefront/pages').then(m => m.ProductsPage) },
      { path: 'products/:id', loadComponent: () => import('./features/storefront/pages').then(m => m.ProductPage) },
      { path: 'cart', loadComponent: () => import('./features/cart/pages').then(m => m.CartPage) },
      { path: 'checkout', loadComponent: () => import('./features/cart/pages').then(m => m.CheckoutPage) },
      { path: 'checkout/confirmation', loadComponent: () => import('./features/cart/pages').then(m => m.OrderConfirmationPage) },
      { path: 'login', loadComponent: () => import('./features/account/pages').then(m => m.LoginPage) },
      { path: 'register', loadComponent: () => import('./features/account/pages').then(m => m.RegisterPage) },
      { path: 'account/orders', canActivate: [authGuard], loadComponent: () => import('./features/account/pages').then(m => m.AccountOrdersPage) },
      { path: 'account/orders/:id', canActivate: [authGuard], loadComponent: () => import('./features/account/pages').then(m => m.AccountOrderDetailPage) },
    ]
  },
  {
    path: 'admin', component: AdminLayout, canActivate: [authGuard, roleGuard], data: { roles: ['Admin', 'Salesman'] }, children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminDashboardPage) },
      { path: 'products', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminProductsPage) },
      { path: 'categories', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminCategoriesPage) },
      { path: 'inventory', canActivate: [roleGuard], data: { roles: ['Admin', 'Salesman'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminInventoryPage) },
      { path: 'orders', canActivate: [roleGuard], data: { roles: ['Admin', 'Salesman'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminOrdersPage) },
      { path: 'delivery-zones', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminDeliveryZonesPage) },
      { path: 'coupons', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminCouponsPage) },
      { path: 'reports', canActivate: [roleGuard], data: { roles: ['Admin'] }, loadComponent: () => import('./features/admin/pages').then(m => m.AdminReportsPage) },
      { path: 'pos', canActivate: [roleGuard], data: { roles: ['Salesman'] }, loadComponent: () => import('./features/admin/pages').then(m => m.PosPage) },
    ]
  },
  { path: '**', redirectTo: '' }
];
