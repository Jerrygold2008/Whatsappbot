import { ChevronRight } from "lucide-react";
import { Pill } from "./kit";
import { ago, naira, paymentLabel, paymentTone, statusLabel, statusTone } from "@/lib/format";
import type { Order } from "@/lib/types";

export function OrdersTable({ orders, onOpen }: { orders: Order[]; onOpen: (o: Order) => void }) {
  return (
    <>
      {/* Mobile cards */}
      <ul className="divide-y md:hidden">
        {orders.map((o) => (
          <li key={o.id}>
            <button onClick={() => onOpen(o)} className="flex w-full items-center gap-3 px-4 py-3.5 text-left hover:bg-muted/50">
              <div className="min-w-0 flex-1">
                <div className="flex items-center justify-between gap-2">
                  <span className="truncate font-semibold">{o.customerName ?? `+${o.customerPhone}`}</span>
                  <span className="tabular shrink-0 font-bold">{naira(o.total)}</span>
                </div>
                <p className="mt-0.5 font-mono text-xs text-muted-foreground">{o.orderNumber} · {ago(o.createdAt)}</p>
                <div className="mt-2 flex flex-wrap gap-1.5">
                  <Pill tone={statusTone[o.status]}>{statusLabel[o.status]}</Pill>
                  <Pill tone={paymentTone[o.paymentStatus]}>{paymentLabel[o.paymentStatus]}</Pill>
                </div>
              </div>
              <ChevronRight className="size-4 text-muted-foreground" />
            </button>
          </li>
        ))}
      </ul>
      {/* Desktop table */}
      <table className="hidden w-full text-sm md:table">
        <thead>
          <tr className="border-b bg-muted/40 text-left text-xs font-bold uppercase tracking-wider text-muted-foreground">
            <th className="px-5 py-3">Order</th><th className="px-5 py-3">Customer</th><th className="px-5 py-3">Status</th>
            <th className="px-5 py-3">Payment</th><th className="px-5 py-3 text-right">Total</th><th className="px-5 py-3 text-right">Placed</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {orders.map((o) => (
            <tr key={o.id} onClick={() => onOpen(o)} className="cursor-pointer transition-colors hover:bg-muted/50">
              <td className="whitespace-nowrap px-5 py-3.5 font-mono text-xs font-semibold">{o.orderNumber}</td>
              <td className="px-5 py-3.5">
                <p className="font-semibold">{o.customerName ?? "—"}</p>
                <p className="text-xs text-muted-foreground">+{o.customerPhone}</p>
              </td>
              <td className="px-5 py-3.5"><Pill tone={statusTone[o.status]}>{statusLabel[o.status]}</Pill></td>
              <td className="px-5 py-3.5"><Pill tone={paymentTone[o.paymentStatus]}>{paymentLabel[o.paymentStatus]}</Pill></td>
              <td className="tabular px-5 py-3.5 text-right font-bold">{naira(o.total)}</td>
              <td className="px-5 py-3.5 text-right text-muted-foreground">{ago(o.createdAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
