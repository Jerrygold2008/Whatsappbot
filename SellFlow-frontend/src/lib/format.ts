import type { OrderStatus, PaymentStatus } from "./types";

export const naira = (v: number | null | undefined) =>
  v == null ? "—" : `₦${Number(v).toLocaleString("en-NG", { maximumFractionDigits: 0 })}`;

export const dateTime = (s: string | null | undefined) =>
  s ? new Date(s).toLocaleString("en-NG", { day: "numeric", month: "short", hour: "numeric", minute: "2-digit" }) : "—";

export const ago = (s: string) => {
  const m = Math.round((Date.now() - +new Date(s)) / 60000);
  if (m < 1) return "just now";
  if (m < 60) return `${m}m ago`;
  const h = Math.round(m / 60);
  if (h < 24) return `${h}h ago`;
  return `${Math.round(h / 24)}d ago`;
};

export const phone = (p: string) => (p.startsWith("+") ? p : `+${p}`);

export const statusLabel: Record<OrderStatus, string> = {
  Pending: "Pending", Confirmed: "Confirmed", Processing: "Processing", Dispatched: "In transit",
  Delivered: "Delivered", Cancelled: "Cancelled", Failed: "Delivery failed",
};

export const paymentLabel: Record<PaymentStatus, string> = {
  Unpaid: "Pay on delivery", Pending: "Awaiting transfer", ProofSubmitted: "Receipt to review",
  Successful: "Paid", Failed: "Failed", NeedsRefund: "Refund due", Refunded: "Refunded",
};

export type Tone = "neutral" | "info" | "success" | "warning" | "danger" | "brand";

export const statusTone: Record<OrderStatus, Tone> = {
  Pending: "warning", Confirmed: "brand", Processing: "info", Dispatched: "info",
  Delivered: "success", Cancelled: "neutral", Failed: "danger",
};
export const paymentTone: Record<PaymentStatus, Tone> = {
  Unpaid: "neutral", Pending: "warning", ProofSubmitted: "warning", Successful: "success",
  Failed: "danger", NeedsRefund: "danger", Refunded: "neutral",
};

export const nextSteps = (status: OrderStatus, method: string, pay: PaymentStatus): { to: OrderStatus; label: string }[] => {
  switch (status) {
    case "Pending":
      return method === "PayOnDelivery" || pay === "Successful"
        ? [{ to: "Confirmed", label: "Accept order" }, { to: "Cancelled", label: "Decline" }]
        : [{ to: "Cancelled", label: "Cancel order" }];
    case "Confirmed": return [{ to: "Processing", label: "Mark processing" }, { to: "Cancelled", label: "Cancel" }];
    case "Processing": return [{ to: "Dispatched", label: "Mark in transit" }, { to: "Cancelled", label: "Cancel" }];
    case "Dispatched": return [{ to: "Delivered", label: "Mark delivered" }, { to: "Failed", label: "Delivery failed" }];
    default: return [];
  }
};
