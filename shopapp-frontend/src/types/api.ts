// ── Enums (match backend numeric enums) ─────────────────────────────
export enum ItemStatus {
  Active = 0,
  Inactive = 1,
  Sold = 2,
}

export enum ItemCondition {
  New = 0,
  Used = 1,
  Refurbished = 2,
}

export enum OrderStatus {
  Pending = 0,
  Confirmed = 1,
  Shipped = 2,
  Delivered = 3,
  Cancelled = 4,
  Refunded = 5,
}

export enum PaymentStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Refunded = 4,
}

export enum UserStatus {
  Active = 0,
  Banned = 1,
  TimedOut = 2,
}

// ── Auth ─────────────────────────────────────────────────────────────
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

// ── Items ────────────────────────────────────────────────────────────
export interface ItemSummaryDto {
  id: string;
  title: string;
  price: number;
  status: ItemStatus;
  condition: ItemCondition;
  location: string | null;
  createdAt: string;
  categoryName: string;
  sellerName: string;
  primaryPhotoUrl: string | null;
}

export interface ItemPhotoDto {
  id: string;
  url: string;
  altText: string | null;
  isPrimary: boolean;
  order: number;
}

export interface ItemDto extends ItemSummaryDto {
  description: string;
  quantity: number;
  viewCount: number;
  expiresAt: string | null;
  categoryId: string;
  sellerId: string;
  photos: ItemPhotoDto[];
}

export interface CreateItemRequest {
  title: string;
  description: string;
  price: number;
  quantity: number;
  condition: ItemCondition;
  location: string;
  categoryId: string;
  expiresAt?: string | null;
}

export interface UpdateItemRequest {
  title?: string;
  description?: string;
  price?: number;
  quantity?: number;
  condition?: ItemCondition;
  location?: string;
  categoryId?: string;
  status?: ItemStatus;
  expiresAt?: string | null;
}

export interface ItemQueryParams {
  page?: number;
  pageSize?: number;
  categoryId?: string;
  search?: string;
  minPrice?: number;
  maxPrice?: number;
}

// ── Paged Result ─────────────────────────────────────────────────────
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ── Categories ───────────────────────────────────────────────────────
export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  parentCategoryId: string | null;
  subCategories: CategoryDto[];
}

export interface CreateCategoryRequest {
  name: string;
  slug: string;
  description?: string;
  parentCategoryId?: string;
}

export interface UpdateCategoryRequest {
  name?: string;
  slug?: string;
  description?: string;
}

// ── Cart ─────────────────────────────────────────────────────────────
export interface CartItemDto {
  id: string;
  itemId: string;
  itemTitle: string;
  itemPhotoUrl: string | null;
  unitPrice: number;
  quantity: number;
  subTotal: number;
}

export interface CartDto {
  id: string;
  items: CartItemDto[];
  totalAmount: number;
  totalItems: number;
}

export interface AddToCartRequest {
  itemId: string;
  quantity: number;
}

export interface UpdateCartItemRequest {
  quantity: number;
}

// ── Orders ───────────────────────────────────────────────────────────
export interface OrderItemDto {
  id: string;
  itemId: string;
  itemTitle: string;
  quantity: number;
  unitPrice: number;
  subTotal: number;
}

export interface OrderDto {
  id: string;
  orderNumber: string;
  status: OrderStatus;
  paymentStatus: PaymentStatus;
  totalAmount: number;
  notes: string | null;
  shippingFirstName: string;
  shippingLastName: string;
  shippingAddress: string;
  shippingCity: string;
  shippingPostalCode: string;
  shippingCountry: string;
  createdAt: string;
  items: OrderItemDto[];
}

export interface CreateOrderRequest {
  firstName: string;
  lastName: string;
  address: string;
  city: string;
  postalCode: string;
  country?: string;
  notes?: string;
}

// ── Payments ─────────────────────────────────────────────────────────
export interface PaymentStatusDto {
  paymentId: string;
  orderId: string;
  status: PaymentStatus;
  amount: number;
  currency: string;
  provider: string;
  redirectUrl: string | null;
  createdAt: string;
  completedAt: string | null;
}

// ── Users ────────────────────────────────────────────────────────────
export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl: string | null;
  status: UserStatus;
  roles: string[];
  createdAt: string;
}

export interface UpdateUserRequest {
  firstName?: string;
  lastName?: string;
}

export interface BanUserRequest {
  reason: string;
}

export interface SetTimeoutRequest {
  until: string;
  reason?: string;
}

export interface AssignRoleRequest {
  roleName: string;
}

// ── Chatbot ──────────────────────────────────────────────────────────
export interface ChatbotRequest {
  question: string;
  context?: string;
}

export interface ChatbotResponse {
  answer: string;
}

// ── API Error ────────────────────────────────────────────────────────
export interface ApiError {
  status?: number;
  error?: string;
  errors?: Record<string, string[]>;
}
