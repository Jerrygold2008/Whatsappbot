import { ApiError } from "./api";
import { makeDemo } from "./demo";
import type { OrderStatus } from "./types";

// Sample-data backend for the public /demo page. Nothing here talks to a server.
let demo: ReturnType<typeof makeDemo> | null = null;
export const resetDemo = () => { demo = null; };
const allowed: Record<OrderStatus, OrderStatus[]> = {
  Pending: ["Confirmed", "Cancelled"], Confirmed: ["Processing", "Cancelled"], Processing: ["Dispatched", "Cancelled"],
  Dispatched: ["Delivered", "Failed"], Delivered: [], Cancelled: [], Failed: [],
};

export async function demoRequest<T>(method: string, path: string, body: any): Promise<T> {
  demo ??= makeDemo();
  const d = demo;
  await new Promise((r) => setTimeout(r, 180));
  const [p = "", q = ""] = path.split("?");
  const seg = p.split("/").filter(Boolean).slice(1); // drop "api"
  const key = `${method} /${seg.map((s) => (/^(o|p|c)\d+$|^[0-9a-f-]{36}$/.test(s) ? ":id" : s)).join("/")}`;
  const id = seg.find((s) => /^(o|p|c)\d+$/.test(s));
  const order = () => {
    const o = d.orders.find((x) => x.id === id);
    if (!o) throw new ApiError(404, "Order not found.");
    return o;
  };
  const r = (v: unknown) => v as T;
  switch (key) {
    case "POST /auth/login": case "POST /auth/register":
      return r({ token: "demo", businessId: "b1", email: body.email });
    case "POST /businesses": return r({ business: d.business, token: "demo" });
    case "GET /businesses/me": return r(d.business);
    case "PUT /businesses/me": Object.assign(d.business, body); return r(d.business);
    case "PUT /businesses/me/settings": d.business.settings = body; return r(d.business);
    case "GET /store/readiness": return r({ isLive: true, missing: [] });
    case "GET /dashboard/overview": {
      const today = d.orders.filter((o) => Date.now() - +new Date(o.createdAt) < 86400_000);
      return r({
        todaysOrders: today.length, todaysRevenue: today.filter((o) => o.status !== "Cancelled").reduce((s, o) => s + o.total, 0),
        pendingOrders: d.orders.filter((o) => o.status === "Pending").length, totalProducts: d.products.filter((x) => x.isActive).length,
        lowStockProducts: d.products.filter((x) => x.isActive && x.stockQuantity - x.reservedQuantity <= 5).length,
      });
    }
    case "GET /dashboard/customers": return r(d.customers);
    case "GET /dashboard/payments": return r(d.payments);
    case "GET /orders": {
      const s = new URLSearchParams(q).get("status");
      return r(s ? d.orders.filter((o) => o.status === s) : d.orders);
    }
    case "GET /orders/:id": return r(order());
    case "PUT /orders/:id/status": {
      const o = order();
      if (!allowed[o.status].includes(body.status)) throw new ApiError(400, `Cannot move order from ${o.status} to ${body.status}.`);
      if (body.status === "Confirmed" && o.paymentMethod === "BankTransfer" && o.paymentStatus !== "Successful")
        throw new ApiError(400, "Confirm the customer's payment first.");
      o.status = body.status;
      if (body.status === "Delivered" && o.paymentMethod === "PayOnDelivery") o.paymentStatus = "Successful";
      return r(undefined);
    }
    case "POST /orders/:id/payment/confirm": { const o = order(); o.paymentStatus = "Successful"; o.status = "Confirmed"; return r(undefined); }
    case "POST /orders/:id/payment/reject": { const o = order(); o.paymentStatus = "Pending"; return r(undefined); }
    case "GET /orders/:id/proofs": return r(order().proofCount ? [{ id: "pr1", uploadedAt: order().createdAt, contentType: "image/svg+xml" }] : []);
    case "GET /orders/:id/proofs/:id": {
      const o = order();
      const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="360" height="520"><rect width="100%" height="100%" fill="#f4f7f5"/><text x="30" y="60" font-family="sans-serif" font-size="22" font-weight="700" fill="#0f5c45">Transfer Successful</text><text x="30" y="110" font-family="sans-serif" font-size="15" fill="#333">Amount: NGN ${o.total.toLocaleString()}</text><text x="30" y="140" font-family="sans-serif" font-size="15" fill="#333">To: Lumiere Beauty Ltd</text><text x="30" y="170" font-family="sans-serif" font-size="15" fill="#333">Narration: ${o.orderNumber}</text><text x="30" y="200" font-family="sans-serif" font-size="13" fill="#777">Sample receipt (demo)</text></svg>`;
      return r(new Blob([svg], { type: "image/svg+xml" }));
    }
    case "GET /products": return r(d.products);
    case "POST /products": { const n = { ...body, id: `p${Date.now() % 100000}`, reservedQuantity: 0 }; d.products.unshift(n); return r(n); }
    case "PUT /products/:id": { const x = d.products.find((y) => y.id === id)!; Object.assign(x, body); return r(x); }
    case "DELETE /products/:id": { d.products.find((y) => y.id === id)!.isActive = false; return r(undefined); }
    case "POST /products/:id/image": {
      const x = d.products.find((y) => y.id === id)!;
      x.imageUrl = URL.createObjectURL((body as FormData).get("file") as File);
      return r(x);
    }
    case "GET /categories": return r(d.categories);
    case "POST /categories": { const c = { id: `c${Date.now() % 100000}`, name: body.name }; d.categories.push(c); return r(c); }
    case "DELETE /categories/:id": d.categories = d.categories.filter((c) => c.id !== id); return r(undefined);
    case "GET /subscription/plans": return r([{ id: "pl1", name: "Monthly", price: 5000, durationInDays: 30 }, { id: "pl2", name: "Yearly", price: 50000, durationInDays: 365 }]);
    case "GET /subscription": return r({ isActive: true, endDate: new Date(Date.now() + 21 * 86400_000).toISOString(), planName: "Monthly" });
    case "POST /subscription/pay": throw new ApiError(400, "Payments are disabled in demo mode.");
  }
  throw new ApiError(404, `Demo: ${key} not available`);
}
