import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Search, ShoppingBag } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Empty, PageHeader, Panel } from "@/components/app/kit";
import { OrdersTable } from "@/components/app/OrdersTable";
import { OrderSheet } from "@/components/app/OrderSheet";
import { api } from "@/lib/api";
import type { Order } from "@/lib/types";
import { cn } from "@/lib/utils";


const tabs = [
  { key: "all", label: "All" },
  { key: "review", label: "To review" },
  { key: "Pending", label: "Pending" },
  { key: "active", label: "In progress" },
  { key: "Delivered", label: "Delivered" },
  { key: "closed", label: "Cancelled" },
] as const;

export function OrdersView() {
  const [tab, setTab] = useState<(typeof tabs)[number]["key"]>("all");
  const [q, setQ] = useState("");
  const [open, setOpen] = useState<Order | null>(null);
  const { data = [], isLoading } = useQuery({ queryKey: ["orders"], queryFn: () => api.orders() });

  const match = (o: Order) => {
    switch (tab) {
      case "review": return o.status === "Pending" && o.paymentStatus === "ProofSubmitted";
      case "Pending": return o.status === "Pending";
      case "active": return ["Confirmed", "Processing", "Dispatched"].includes(o.status);
      case "Delivered": return o.status === "Delivered";
      case "closed": return o.status === "Cancelled" || o.status === "Failed";
      default: return true;
    }
  };
  const list = useMemo(() => data.filter(match).filter((o) =>
    !q || `${o.orderNumber} ${o.customerName ?? ""} ${o.customerPhone}`.toLowerCase().includes(q.toLowerCase())), [data, tab, q]);

  return (
    <>
      <PageHeader title="Orders" subtitle="Confirm payments and move orders through delivery. Customers are notified on WhatsApp automatically." />
      <Panel>
        <div className="flex flex-col gap-3 border-b p-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="-mx-1 flex gap-1 overflow-x-auto px-1">
            {tabs.map((t) => (
              <button key={t.key} onClick={() => setTab(t.key)}
                className={cn("whitespace-nowrap rounded-lg px-3 py-1.5 text-sm font-semibold transition-colors",
                  tab === t.key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted")}>
                {t.label}
              </button>
            ))}
          </div>
          <div className="relative sm:w-64">
            <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input placeholder="Search order or customer" value={q} onChange={(e) => setQ(e.target.value)} className="pl-9" />
          </div>
        </div>
        {isLoading ? <div className="h-64 animate-pulse bg-muted/40" />
          : list.length ? <OrdersTable orders={list} onOpen={setOpen} />
          : <Empty icon={<ShoppingBag className="size-5" />} title="No orders here" body="Try another filter." />}
      </Panel>
      <OrderSheet order={open} onClose={() => setOpen(null)} />
    </>
  );
}
