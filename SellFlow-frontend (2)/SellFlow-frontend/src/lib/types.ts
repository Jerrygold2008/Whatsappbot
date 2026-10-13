export type OrderStatus =
  | "Pending"
  | "Confirmed"
  | "Processing"
  | "Dispatched"
  | "Delivered"
  | "Cancelled"
  | "Failed";
export type PaymentStatus =
  | "Unpaid"
  | "Pending"
  | "ProofSubmitted"
  | "Successful"
  | "Failed"
  | "NeedsRefund"
  | "Refunded";
export type PaymentMethod = "BankTransfer" | "PayOnDelivery";

export interface AuthResponse {
  token: string;
  businessId: string | null;
  email: string;
}
export interface OrderItem {
  productName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}
export interface Delivery {
  recipientName: string;
  phone: string;
  state: string;
  city: string;
  address: string;
  landmark: string | null;
  status: OrderStatus;
}
export interface Order {
  id: string;
  orderNumber: string;
  customerName: string | null;
  customerPhone: string;
  subtotal: number;
  deliveryFee: number;
  total: number;
  paymentMethod: PaymentMethod;
  paymentStatus: PaymentStatus;
  status: OrderStatus;
  createdAt: string;
  items: OrderItem[];
  delivery: Delivery | null;
  proofCount: number;
}
export interface Product {
  id: string;
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  reservedQuantity: number;
  categoryId: string | null;
  imageUrl: string | null;
  isActive: boolean;
}
export interface ProductRequest {
  name: string;
  description: string | null;
  price: number;
  stockQuantity: number;
  categoryId: string | null;
  imageUrl: string | null;
  isActive: boolean;
}
export interface Category {
  id: string;
  name: string;
}
export interface Overview {
  todaysOrders: number;
  todaysRevenue: number;
  pendingOrders: number;
  totalProducts: number;
  lowStockProducts: number;
}
export interface Customer {
  id: string;
  fullName: string | null;
  whatsAppNumber: string;
  orderCount: number;
  totalSpent: number;
  lastOrderAt: string | null;
}
export interface Payment {
  reference: string;
  orderNumber: string | null;
  amount: number;
  status: PaymentStatus;
  method: PaymentMethod | null;
  paidAt: string | null;
  createdAt: string;
}
export interface Settings {
  acceptBankTransfer: boolean;
  acceptPayOnDelivery: boolean;
  deliveryFee: number | null;
  maxPodOrderValue: number | null;
  podRequiresReturningCustomer: boolean;
  bankName: string | null;
  accountNumber: string | null;
  accountName: string | null;
  ownerWhatsAppNumber: string | null;
}
export interface Business {
  id: string;
  name: string;
  description: string | null;
  whatsAppNumber: string | null;
  whatsAppConnected: boolean;
  settings: Settings;
}
export interface Readiness {
  isLive: boolean;
  missing: string[];
}
export interface Plan {
  id: string;
  name: string;
  price: number;
  durationInDays: number;
}
export interface SubscriptionStatus {
  isActive: boolean;
  endDate: string | null;
  planName: string | null;
}
export interface Proof {
  id: string;
  uploadedAt: string;
  contentType: string;
}
