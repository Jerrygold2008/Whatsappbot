import type {
  AuthResponse, Business, Category, Customer, Order, OrderStatus, Overview, Payment, Plan, Product,
  ProductRequest, Proof, Readiness, Settings, SubscriptionStatus,
} from "./types";

const TOKEN_KEY = "sf_token";
const EMAIL_KEY = "sf_email";
const BUSINESS_KEY = "sf_has_business";

// The API address comes from the build environment (VITE_API_URL), never from the UI.
const API_URL = ((import.meta.env["VITE_API_URL"] as string | undefined) ?? "").replace(/\/+$/, "");

const hasWindow = () => typeof window !== "undefined";

export const storage = {
  apiUrl: () => API_URL,
  token: () => (hasWindow() ? localStorage.getItem(TOKEN_KEY) : null),
  email: () => (hasWindow() ? localStorage.getItem(EMAIL_KEY) : null),
  hasBusiness: () => hasWindow() && localStorage.getItem(BUSINESS_KEY) === "1",
  setSession: (token: string, email: string, hasBusiness: boolean) => {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(EMAIL_KEY, email);
    localStorage.setItem(BUSINESS_KEY, hasBusiness ? "1" : "0");
  },
  clear: () => {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EMAIL_KEY);
    localStorage.removeItem(BUSINESS_KEY);
  },
};

// Demo mode is switched on only by the public /demo page, which uses its own sample data.
let demoMode = false;
export const setDemoMode = (on: boolean) => { demoMode = on; };

export class ApiError extends Error {
  constructor(public status: number, message: string) { super(message); }
}

async function request<T>(method: string, path: string, body?: unknown): Promise<T> {
  if (demoMode) {
    const { demoRequest } = await import("./demo-api");
    return demoRequest<T>(method, path, body);
  }
  const base = storage.apiUrl();
  if (!base) throw new ApiError(0, "This app is not connected to its server yet. Set VITE_API_URL and rebuild.");

  const headers: Record<string, string> = {};
  const token = storage.token();
  if (token) headers["Authorization"] = `Bearer ${token}`;
  const isForm = body instanceof FormData;
  if (body !== undefined && !isForm) headers["Content-Type"] = "application/json";

  let res: Response;
  try {
    res = await fetch(`${base}${path}`, {
      method, headers, body: body === undefined ? null : isForm ? (body as FormData) : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, "Can't reach the server. Check your internet connection and try again.");
  }
  if (res.status === 401) {
    storage.clear();
    throw new ApiError(401, "Your session has expired. Please sign in again.");
  }
  if (!res.ok) {
    let msg = `Request failed (${res.status})`;
    try {
      const j = await res.json();
      msg = j.error ?? j.message ?? j.title ?? msg;
    } catch { /* not json */ }
    throw new ApiError(res.status, msg);
  }
  if (res.status === 204) return undefined as T;
  const type = res.headers.get("content-type") ?? "";
  return (type.includes("json") ? res.json() : res.blob()) as Promise<T>;
}

export const api = {
  login: (email: string, password: string) => request<AuthResponse>("POST", "/api/auth/login", { email, password }),
  register: (b: { firstName: string; lastName: string; email: string; password: string }) =>
    request<AuthResponse>("POST", "/api/auth/register", b),
  createBusiness: (name: string, description: string) =>
    request<{ business: Business; token: string }>("POST", "/api/businesses", { name, description }),
  business: () => request<Business>("GET", "/api/businesses/me"),
  updateBusiness: (name: string, description: string | null) => request<Business>("PUT", "/api/businesses/me", { name, description }),
  updateSettings: (s: Settings) => request<Business>("PUT", "/api/businesses/me/settings", s),
  readiness: () => request<Readiness>("GET", "/api/store/readiness"),
  overview: () => request<Overview>("GET", "/api/dashboard/overview"),
  customers: () => request<Customer[]>("GET", "/api/dashboard/customers"),
  payments: () => request<Payment[]>("GET", "/api/dashboard/payments"),
  orders: (status?: OrderStatus) => request<Order[]>("GET", `/api/orders${status ? `?status=${status}` : ""}`),
  order: (id: string) => request<Order>("GET", `/api/orders/${id}`),
  setStatus: (id: string, status: OrderStatus) => request<void>("PUT", `/api/orders/${id}/status`, { status }),
  confirmPayment: (id: string) => request<void>("POST", `/api/orders/${id}/payment/confirm`),
  rejectPayment: (id: string, reason: string) => request<void>("POST", `/api/orders/${id}/payment/reject`, { reason }),
  proofs: (id: string) => request<Proof[]>("GET", `/api/orders/${id}/proofs`),
  proofBlob: (id: string, proofId: string) => request<Blob>("GET", `/api/orders/${id}/proofs/${proofId}`),
  products: () => request<Product[]>("GET", "/api/products?includeInactive=true"),
  createProduct: (p: ProductRequest) => request<Product>("POST", "/api/products", p),
  updateProduct: (id: string, p: ProductRequest) => request<Product>("PUT", `/api/products/${id}`, p),
  deactivateProduct: (id: string) => request<void>("DELETE", `/api/products/${id}`),
  uploadImage: (id: string, file: File) => {
    const f = new FormData();
    f.append("file", file);
    return request<Product>("POST", `/api/products/${id}/image`, f);
  },
  categories: () => request<Category[]>("GET", "/api/categories"),
  createCategory: (name: string) => request<Category>("POST", "/api/categories", { name }),
  deleteCategory: (id: string) => request<void>("DELETE", `/api/categories/${id}`),
  plans: () => request<Plan[]>("GET", "/api/subscription/plans"),
  subscription: () => request<SubscriptionStatus>("GET", "/api/subscription"),
  pay: (planId?: string) => request<{ paymentUrl: string }>("POST", "/api/subscription/pay", { planId }),
};
