export interface TokenResponse { accessToken: string; expiresAtUtc: string; tokenType: string; }
export interface CurrentUser { id: string; email: string; displayName: string; roles: string[]; }
export interface Category { id: string; name: string; slug: string; description?: string | null; parentCategoryId?: string | null; isActive: boolean; sortOrder: number; }
export interface CategoryWrite { name: string; slug: string; description: string | null; parentCategoryId: string | null; sortOrder: number; }
export interface Product { id: string; name: string; sku: string; slug: string; description?: string | null; brand?: string | null; categoryId: string; categoryName: string; unit: string; price: number; reorderLevel: number; isActive: boolean; imageUrl?: string | null; }
export interface ProductWrite { name: string; sku: string; categoryId: string; unit: string; price: number; reorderLevel: number; description: string | null; brand: string | null; }
export interface Page<T> { items: T[]; pageNumber: number; pageSize: number; totalCount: number; }
export interface CartLine { productId: string; name: string; sku: string; unit: string; unitPrice: number; quantity: number; availableQuantity: number; lineTotal: number; imageUrl?: string | null; }
export interface Cart { items: CartLine[]; subtotal: number; currency: string; }
export interface DeliveryZone { id: string; code: string; name: string; fee: number; isActive: boolean; }
export interface CheckoutRequest { customerName: string; customerPhone: string; customerEmail: string | null; addressLine: string; deliveryZoneCode: string; deliveryNotes: string | null; couponCode: string | null; }
export interface OrderLine { sku: string; name: string; unit: string; unitPrice: number; quantity: number; lineTotal: number; }
export interface Order { id: string; orderNumber: string; customerName: string; customerPhone: string; customerEmail?: string | null; addressLine: string; area: string; deliveryNotes?: string | null; channel: number; status: number; paymentMethod: number; paymentStatus: number; couponCode?: string | null; subtotal: number; discountTotal: number; deliveryFee: number; taxTotal: number; total: number; currency: string; createdAtUtc: string; items: OrderLine[]; }
export interface CreatedOrder { order: Order; guestTrackingToken?: string | null; }
export interface TrackingRequest { orderNumber: string; trackingToken: string; }
export interface InventorySummary { productId: string; productName: string; sku: string; onHand: number; available: number; reorderLevel: number; nearestExpiry?: string | null; }
export interface InventoryBatch { id: string; productId: string; productName: string; sku: string; batchNumber: string; receivedAtUtc: string; expiryDate?: string | null; initialQuantity: number; remainingQuantity: number; reservedQuantity: number; availableQuantity: number; unitCost?: number | null; }
export interface StockMovement { id: number; productId: string; batchId?: string | null; quantityChange: number; reason: number; actorUserId?: string | null; createdAtUtc: string; notes?: string | null; }
export interface ReceiptRequest { productId: string; batchNumber: string; expiryDate: string | null; quantity: number; unitCost: number | null; notes: string | null; }
export interface Coupon { id: string; code: string; discountType: number; value: number; minimumOrderAmount: number; maximumDiscount?: number | null; startsAtUtc: string; endsAtUtc: string; usageLimit?: number | null; perCustomerLimit?: number | null; isActive: boolean; }
export interface CouponQuote { code: string; discountAmount: number; payableBeforeDelivery: number; }
export interface CouponWrite { code: string; discountType: number; value: number; minimumOrderAmount: number; maximumDiscount: number | null; startsAtUtc: string; endsAtUtc: string; usageLimit: number | null; perCustomerLimit: number | null; }
export interface SalesReport { fromUtc: string; toUtc: string; paidOrderCount: number; salesTotal: number; discountTotal: number; deliveryTotal: number; byChannel: { channel: string; orderCount: number; salesTotal: number }[]; byDay: { date: string; orderCount: number; salesTotal: number }[]; topProducts: { sku: string; name: string; quantitySold: number; salesTotal: number }[]; byStaff: { staffUserId: string; orderCount: number; salesTotal: number }[]; }

export const ORDER_STATUS: Record<number, string> = { 1: 'Pending', 2: 'Confirmed', 3: 'Packed', 4: 'Out for delivery', 5: 'Delivered', 6: 'Completed', 7: 'Cancelled' };
export const STOCK_REASONS: Record<number, string> = { 1: 'Receipt', 2: 'Sale', 3: 'Damage', 4: 'Expired disposal', 5: 'Stocktake correction', 6: 'Manual adjustment' };
