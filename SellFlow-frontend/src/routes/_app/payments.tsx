import { createFileRoute } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Wallet } from "lucide-react";
import { Empty, PageHeader, Panel, Pill } from "@/components/app/kit";
import { api } from "@/lib/api";
import { dateTime, naira, paymentLabel, paymentTone } from "@/lib/format";

export const Route = createFileRoute("/_app/payments")({
  component: Payments,
  head: () => ({
    meta: [
      { title: "Payments — SellFlow" },
      { name: "description", content: "Track every transfer and payment for your store." },
      { property: "og:title", content: "Payments — SellFlow" },
      { property: "og:description", content: "Track every transfer and payment for your store." },
    ],
  }),
});

function Payments() {
  const { data = [] } = useQuery({ queryKey: ["payments"], queryFn: api.payments });
  const received = data.filter((p) => p.status === "Successful").reduce((s, p) => s + p.amount, 0);
  return (
    <>
      <PageHeader title="Payments" subtitle={`${naira(received)} received from ${data.filter((p) => p.status === "Successful").length} payments.`} />
      <Panel>
        {!data.length ? <Empty icon={<Wallet className="size-5" />} title="No payments yet" /> : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[600px] text-sm">
              <thead><tr className="border-b bg-muted/40 text-left text-xs font-bold uppercase tracking-wider text-muted-foreground">
                <th className="px-5 py-3">Reference</th><th className="px-5 py-3">Order</th><th className="px-5 py-3">Status</th><th className="px-5 py-3 text-right">Amount</th><th className="px-5 py-3 text-right">Date</th>
              </tr></thead>
              <tbody className="divide-y">
                {data.map((p) => (
                  <tr key={p.reference}>
                    <td className="px-5 py-3.5 font-mono text-xs">{p.reference}</td>
                    <td className="px-5 py-3.5 font-mono text-xs text-muted-foreground">{p.orderNumber ?? "Subscription"}</td>
                    <td className="px-5 py-3.5"><Pill tone={paymentTone[p.status]}>{paymentLabel[p.status]}</Pill></td>
                    <td className="tabular px-5 py-3.5 text-right font-bold">{naira(p.amount)}</td>
                    <td className="px-5 py-3.5 text-right text-muted-foreground">{dateTime(p.paidAt ?? p.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Panel>
    </>
  );
}
