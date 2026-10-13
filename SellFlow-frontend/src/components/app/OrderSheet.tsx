import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Check, X, MapPin, Phone, Receipt, MessageCircle } from "lucide-react";
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { Pill } from "./kit";
import { api } from "@/lib/api";
import { dateTime, naira, nextSteps, paymentLabel, paymentTone, phone, statusLabel, statusTone } from "@/lib/format";
import type { Order, OrderStatus } from "@/lib/types";
import { cn } from "@/lib/utils";

const flow: OrderStatus[] = ["Pending", "Confirmed", "Processing", "Dispatched", "Delivered"];

function ProofImage({ orderId, proofId }: { orderId: string; proofId: string }) {
  const [url, setUrl] = useState<string | null>(null);
  const [isPdf, setIsPdf] = useState(false);
  useEffect(() => {
    let u: string | null = null;
    api.proofBlob(orderId, proofId).then((b) => { u = URL.createObjectURL(b); setIsPdf(b.type === "application/pdf"); setUrl(u); }).catch(() => setUrl(null));
    return () => { if (u) URL.revokeObjectURL(u); };
  }, [orderId, proofId]);
  if (!url) return <div className="h-48 animate-pulse rounded-lg bg-muted" />;
  return isPdf
    ? <a href={url} target="_blank" rel="noreferrer" className="text-sm font-semibold text-primary underline">Open PDF receipt</a>
    : <a href={url} target="_blank" rel="noreferrer"><img src={url} alt="Payment receipt" className="max-h-80 w-full rounded-lg border bg-muted object-contain" /></a>;
}

export function OrderSheet({ order, onClose }: { order: Order | null; onClose: () => void }) {
  const qc = useQueryClient();
  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState("We could not verify this transfer in our account.");
  const live = useQuery({ queryKey: ["order", order?.id], queryFn: () => api.order(order!.id), enabled: !!order, initialData: order ?? undefined });
  const o = live.data ?? order;
  const proofs = useQuery({ queryKey: ["proofs", o?.id], queryFn: () => api.proofs(o!.id), enabled: !!o && o.proofCount > 0 });

  const refresh = () => {
    qc.invalidateQueries({ queryKey: ["orders"] });
    qc.invalidateQueries({ queryKey: ["order", o?.id] });
    qc.invalidateQueries({ queryKey: ["overview"] });
    qc.invalidateQueries({ queryKey: ["payments"] });
  };
  const status = useMutation({
    mutationFn: (to: OrderStatus) => api.setStatus(o!.id, to),
    onSuccess: (_, to) => { toast.success(`Order marked ${statusLabel[to].toLowerCase()}. Customer notified on WhatsApp.`); refresh(); },
    onError: (e: Error) => toast.error(e.message),
  });
  const confirm = useMutation({
    mutationFn: () => api.confirmPayment(o!.id),
    onSuccess: () => { toast.success("Payment confirmed. Customer notified on WhatsApp."); refresh(); },
    onError: (e: Error) => toast.error(e.message),
  });
  const reject = useMutation({
    mutationFn: () => api.rejectPayment(o!.id, reason),
    onSuccess: () => { toast.success("Payment rejected. Customer asked for a new receipt."); setRejecting(false); refresh(); },
    onError: (e: Error) => toast.error(e.message),
  });

  const reviewable = o && o.paymentMethod === "BankTransfer" && o.status === "Pending" && (o.paymentStatus === "ProofSubmitted" || o.paymentStatus === "Pending");
  const steps = o ? nextSteps(o.status, o.paymentMethod, o.paymentStatus) : [];
  const stepIndex = o ? flow.indexOf(o.status) : -1;

  return (
    <Sheet open={!!order} onOpenChange={(v) => !v && onClose()}>
      <SheetContent className="w-full overflow-y-auto p-0 sm:max-w-lg">
        {o && (
          <>
            <SheetHeader className="border-b p-5 text-left">
              <SheetTitle className="font-mono text-base">{o.orderNumber}</SheetTitle>
              <SheetDescription>{dateTime(o.createdAt)}</SheetDescription>
              <div className="flex flex-wrap gap-2 pt-1">
                <Pill tone={statusTone[o.status]}>{statusLabel[o.status]}</Pill>
                <Pill tone={paymentTone[o.paymentStatus]}>{paymentLabel[o.paymentStatus]}</Pill>
              </div>
            </SheetHeader>

            <div className="space-y-6 p-5">
              {stepIndex >= 0 && (
                <ol className="grid grid-cols-5 gap-1">
                  {flow.map((s, i) => (
                    <li key={s} className="space-y-1.5">
                      <div className={cn("h-1.5 rounded-full", i <= stepIndex ? "bg-primary" : "bg-muted")} />
                      <p className={cn("text-[11px] font-semibold", i <= stepIndex ? "text-foreground" : "text-muted-foreground")}>{statusLabel[s]}</p>
                    </li>
                  ))}
                </ol>
              )}

              {reviewable && (
                <div className="rounded-xl border border-warning/50 bg-warning/10 p-4">
                  <div className="mb-3 flex items-center gap-2 text-sm font-bold"><Receipt className="size-4" /> Payment review</div>
                  {proofs.data?.length ? (
                    <div className="space-y-2">{proofs.data.slice(0, 1).map((p) => <ProofImage key={p.id} orderId={o.id} proofId={p.id} />)}</div>
                  ) : (
                    <p className="text-sm text-muted-foreground">No receipt yet. The customer will send it to your store on WhatsApp.</p>
                  )}
                  {rejecting ? (
                    <div className="mt-3 space-y-2">
                      <Textarea rows={2} value={reason} onChange={(e) => setReason(e.target.value)} className="bg-card" />
                      <div className="flex gap-2">
                        <Button variant="destructive" size="sm" onClick={() => reject.mutate()} disabled={reject.isPending}>Send rejection</Button>
                        <Button variant="ghost" size="sm" onClick={() => setRejecting(false)}>Back</Button>
                      </div>
                    </div>
                  ) : (
                    <div className="mt-3 grid grid-cols-2 gap-2">
                      <Button variant="success" onClick={() => confirm.mutate()} disabled={confirm.isPending}><Check /> Confirm</Button>
                      <Button variant="outline" onClick={() => setRejecting(true)}><X /> Reject</Button>
                    </div>
                  )}
                  <p className="mt-3 text-xs text-muted-foreground">You can also approve this from the buttons sent to your owner WhatsApp number.</p>
                </div>
              )}

              {steps.length > 0 && !reviewable && (
                <div className="space-y-2">
                  <p className="text-xs font-bold uppercase tracking-wider text-muted-foreground">Update order</p>
                  <div className="grid grid-cols-2 gap-2">
                    {steps.map((s, i) => (
                      <Button key={s.to} variant={i === 0 ? "default" : "outline"} disabled={status.isPending} onClick={() => status.mutate(s.to)}>{s.label}</Button>
                    ))}
                  </div>
                </div>
              )}
              {reviewable && steps.length > 0 && o.paymentStatus !== "Successful" && (
                <Button variant="ghost" size="sm" className="text-destructive" onClick={() => status.mutate("Cancelled")}>Cancel this order</Button>
              )}

              <div>
                <p className="mb-2 text-xs font-bold uppercase tracking-wider text-muted-foreground">Items</p>
                <div className="divide-y rounded-lg border">
                  {o.items.map((i) => (
                    <div key={i.productName} className="flex justify-between gap-3 px-3.5 py-2.5 text-sm">
                      <span>{i.productName} <span className="text-muted-foreground">× {i.quantity}</span></span>
                      <span className="tabular font-semibold">{naira(i.lineTotal)}</span>
                    </div>
                  ))}
                  <div className="space-y-1 bg-muted/40 px-3.5 py-2.5 text-sm">
                    <div className="flex justify-between text-muted-foreground"><span>Subtotal</span><span className="tabular">{naira(o.subtotal)}</span></div>
                    <div className="flex justify-between text-muted-foreground"><span>Delivery</span><span className="tabular">{naira(o.deliveryFee)}</span></div>
                    <div className="flex justify-between font-bold"><span>Total</span><span className="tabular">{naira(o.total)}</span></div>
                  </div>
                </div>
                <p className="mt-2 text-xs text-muted-foreground">{o.paymentMethod === "BankTransfer" ? "Bank transfer" : "Pay on delivery"}</p>
              </div>

              <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-lg border p-3.5 text-sm">
                  <p className="mb-1.5 text-xs font-bold uppercase tracking-wider text-muted-foreground">Customer</p>
                  <p className="font-semibold">{o.customerName ?? "—"}</p>
                  <a href={`https://wa.me/${o.customerPhone}`} target="_blank" rel="noreferrer" className="mt-1 inline-flex items-center gap-1.5 text-primary">
                    <MessageCircle className="size-3.5" />{phone(o.customerPhone)}
                  </a>
                </div>
                {o.delivery && (
                  <div className="rounded-lg border p-3.5 text-sm">
                    <p className="mb-1.5 text-xs font-bold uppercase tracking-wider text-muted-foreground">Delivery</p>
                    <p className="font-semibold">{o.delivery.recipientName}</p>
                    <p className="mt-1 flex gap-1.5 text-muted-foreground"><MapPin className="mt-0.5 size-3.5 shrink-0" />{o.delivery.address}, {o.delivery.city}, {o.delivery.state}{o.delivery.landmark ? ` · near ${o.delivery.landmark}` : ""}</p>
                    <p className="mt-1 flex items-center gap-1.5 text-muted-foreground"><Phone className="size-3.5" />{o.delivery.phone}</p>
                  </div>
                )}
              </div>
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}
