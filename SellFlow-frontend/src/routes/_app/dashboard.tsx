import { createFileRoute, Link } from "@tanstack/react-router";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { ShoppingBag, Banknote, Clock, PackageX, Receipt, AlertTriangle, ArrowRight } from "lucide-react";
import { Empty, PageHeader, Panel, Stat } from "@/components/app/kit";
import { OrdersTable } from "@/components/app/OrdersTable";
import { OrderSheet } from "@/components/app/OrderSheet";
import { api } from "@/lib/api";
import { naira } from "@/lib/format";
import type { Order } from "@/lib/types";

export const Route = createFileRoute("/_app/dashboard")({
  component: Dashboard,
  head: () => ({
    meta: [
      { title: "Overview — SellFlow" },
      { name: "description", content: "Today's sales, orders and payments at a glance." },
      { property: "og:title", content: "Overview — SellFlow" },
      { property: "og:description", content: "Today's sales, orders and payments at a glance." },
    ],
  }),
});

function Dashboard() {
  const [open, setOpen] = useState<Order | null>(null);
  const ov = useQuery({ queryKey: ["overview"], queryFn: api.overview });
  const orders = useQuery({ queryKey: ["orders"], queryFn: () => api.orders() });
  const ready = useQuery({ queryKey: ["readiness"], queryFn: api.readiness });
  const products = useQuery({ queryKey: ["products"], queryFn: api.products });
  const review = orders.data?.filter((o) => o.status === "Pending" && o.paymentStatus === "ProofSubmitted") ?? [];
  const low = products.data?.filter((p) => p.isActive && p.stockQuantity - p.reservedQuantity <= 5) ?? [];
  const d = ov.data;

  return (
    <>
      <PageHeader title="Overview" subtitle="Here's what's happening in your store today." />
      {ready.data && !ready.data.isLive && (
        <div className="mb-6 flex gap-3 rounded-xl border border-warning/50 bg-warning/10 p-4 text-sm">
          <AlertTriangle className="size-5 shrink-0 text-warning-foreground" />
          <div><p className="font-bold">Your store is not live yet</p><p className="text-muted-foreground">To finish: {ready.data.missing.join(" · ")}</p></div>
        </div>
      )}
      <div className="grid grid-cols-2 gap-3 sm:gap-4 lg:grid-cols-4">
        <Stat label="Today's revenue" value={naira(d?.todaysRevenue)} icon={<Banknote className="size-4" />} />
        <Stat label="Today's orders" value={d?.todaysOrders ?? "—"} icon={<ShoppingBag className="size-4" />} tone="info" />
        <Stat label="Pending" value={d?.pendingOrders ?? "—"} hint="Need your action" icon={<Clock className="size-4" />} tone="warning" />
        <Stat label="Low stock" value={d?.lowStockProducts ?? "—"} hint={`of ${d?.totalProducts ?? 0} products`} icon={<PackageX className="size-4" />} tone="danger" />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <Panel title="Recent orders" className="lg:col-span-2" action={<Link to="/orders" className="flex items-center gap-1 text-xs font-bold text-primary">View all <ArrowRight className="size-3" /></Link>}>
          {orders.data?.length ? <OrdersTable orders={orders.data.slice(0, 6)} onOpen={setOpen} />
            : <Empty icon={<ShoppingBag className="size-5" />} title="No orders yet" body="Orders placed through your WhatsApp bot appear here." />}
        </Panel>
        <div className="space-y-6">
          <Panel title="Receipts to review">
            {review.length ? (
              <ul className="divide-y">
                {review.map((o) => (
                  <li key={o.id}>
                    <button onClick={() => setOpen(o)} className="flex w-full items-center gap-3 px-5 py-3 text-left hover:bg-muted/50">
                      <span className="grid size-9 place-items-center rounded-lg bg-warning/15 text-warning-foreground"><Receipt className="size-4" /></span>
                      <span className="min-w-0 flex-1"><span className="block truncate text-sm font-semibold">{o.customerName}</span><span className="font-mono text-xs text-muted-foreground">{o.orderNumber}</span></span>
                      <span className="tabular text-sm font-bold">{naira(o.total)}</span>
                    </button>
                  </li>
                ))}
              </ul>
            ) : <p className="px-5 py-6 text-sm text-muted-foreground">All payments reviewed. 🎉</p>}
          </Panel>
          <Panel title="Restock soon" action={<Link to="/products" className="text-xs font-bold text-primary">Manage</Link>}>
            {low.length ? (
              <ul className="divide-y">
                {low.map((p) => {
                  const left = p.stockQuantity - p.reservedQuantity;
                  return (
                    <li key={p.id} className="flex items-center justify-between gap-3 px-5 py-3 text-sm">
                      <span className="truncate">{p.name}</span>
                      <span className={left <= 0 ? "font-bold text-destructive" : "font-bold text-warning-foreground"}>{left <= 0 ? "Out" : `${left} left`}</span>
                    </li>
                  );
                })}
              </ul>
            ) : <p className="px-5 py-6 text-sm text-muted-foreground">Stock levels look healthy.</p>}
          </Panel>
        </div>
      </div>
      <OrderSheet order={open} onClose={() => setOpen(null)} />
    </>
  );
}
