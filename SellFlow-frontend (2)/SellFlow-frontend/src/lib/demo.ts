import type { Business, Category, Customer, Order, Payment, Product } from "./types";

// Sample data for the public /demo page.
const iso = (hoursAgo: number) => new Date(Date.now() - hoursAgo * 3600_000).toISOString();

export function makeDemo() {
  const categories: Category[] = [
    { id: "c1", name: "Fragrance" },
    { id: "c2", name: "Skincare" },
    { id: "c3", name: "Accessories" },
  ];
  const products: Product[] = [
    { id: "p1", name: "Oud Noir Eau de Parfum 50ml", description: "Warm, woody evening scent.", price: 28500, stockQuantity: 14, reservedQuantity: 2, categoryId: "c1", imageUrl: null, isActive: true },
    { id: "p2", name: "Shea Glow Body Butter", description: "Raw shea, vanilla & cocoa.", price: 6500, stockQuantity: 4, reservedQuantity: 1, categoryId: "c2", imageUrl: null, isActive: true },
    { id: "p3", name: "Vitamin C Brightening Serum", description: "15% vitamin C, 30ml.", price: 12000, stockQuantity: 22, reservedQuantity: 0, categoryId: "c2", imageUrl: null, isActive: true },
    { id: "p4", name: "Gold Link Bracelet", description: "18k gold-plated.", price: 9800, stockQuantity: 0, reservedQuantity: 0, categoryId: "c3", imageUrl: null, isActive: true },
    { id: "p5", name: "Black Soap Facial Cleanser", description: "Gentle daily cleanser.", price: 4200, stockQuantity: 38, reservedQuantity: 3, categoryId: "c2", imageUrl: null, isActive: true },
  ];
  const mk = (n: number, name: string, phone: string, items: [string, number, number][], method: Order["paymentMethod"], pay: Order["paymentStatus"], status: Order["status"], h: number, proofs = 0): Order => {
    const sub = items.reduce((s, [, p, q]) => s + p * q, 0);
    return {
      id: `o${n}`, orderNumber: `ORD-${480120 + n}-261006`, customerName: name, customerPhone: phone,
      subtotal: sub, deliveryFee: 2500, total: sub + 2500, paymentMethod: method, paymentStatus: pay, status, createdAt: iso(h),
      items: items.map(([productName, unitPrice, quantity]) => ({ productName, unitPrice, quantity, lineTotal: unitPrice * quantity })),
      delivery: { recipientName: name, phone: `0${phone.slice(3)}`, state: "Lagos", city: "Lekki Phase 1", address: "12 Admiralty Way", landmark: "Opposite Mega Plaza", status },
      proofCount: proofs,
    };
  };
  const orders: Order[] = [
    mk(1, "Adaeze Okafor", "2348031234567", [["Oud Noir Eau de Parfum 50ml", 28500, 1]], "BankTransfer", "ProofSubmitted", "Pending", 0.4, 1),
    mk(2, "Tunde Bakare", "2348059876543", [["Vitamin C Brightening Serum", 12000, 2]], "PayOnDelivery", "Unpaid", "Pending", 1.2),
    mk(3, "Chioma Eze", "2348091112233", [["Shea Glow Body Butter", 6500, 3], ["Black Soap Facial Cleanser", 4200, 1]], "BankTransfer", "Successful", "Confirmed", 3),
    mk(4, "Ibrahim Musa", "2348024445566", [["Gold Link Bracelet", 9800, 1]], "BankTransfer", "Successful", "Processing", 7),
    mk(5, "Folake Adeyemi", "2348067778899", [["Oud Noir Eau de Parfum 50ml", 28500, 2]], "PayOnDelivery", "Unpaid", "Dispatched", 20),
    mk(6, "Emeka Nwosu", "2348013334455", [["Black Soap Facial Cleanser", 4200, 4]], "BankTransfer", "Successful", "Delivered", 50),
    mk(7, "Zainab Bello", "2348085556677", [["Vitamin C Brightening Serum", 12000, 1]], "BankTransfer", "Failed", "Cancelled", 72),
  ];
  const customers: Customer[] = orders.slice(0, 6).map((o, i) => ({
    id: `cu${i}`, fullName: o.customerName, whatsAppNumber: o.customerPhone, orderCount: 1 + (i % 3), totalSpent: o.total * (1 + (i % 3)), lastOrderAt: o.createdAt,
  }));
  const payments: Payment[] = orders.filter((o) => o.paymentMethod === "BankTransfer").map((o) => ({
    reference: `TRF-${o.orderNumber}`, orderNumber: o.orderNumber, amount: o.total, status: o.paymentStatus, method: o.paymentMethod,
    paidAt: o.paymentStatus === "Successful" ? o.createdAt : null, createdAt: o.createdAt,
  }));
  const business: Business = {
    id: "b1", name: "Lumière Beauty Lagos", description: "Premium fragrance & skincare, delivered across Lagos.",
    whatsAppNumber: "+234 812 000 4455", whatsAppConnected: true,
    settings: { acceptBankTransfer: true, acceptPayOnDelivery: true, deliveryFee: 2500, maxPodOrderValue: 60000, podRequiresReturningCustomer: false, bankName: "GTBank", accountNumber: "0123456789", accountName: "Lumiere Beauty Ltd", ownerWhatsAppNumber: "2348030001122" },
  };
  return { categories, products, orders, customers, payments, business };
}
