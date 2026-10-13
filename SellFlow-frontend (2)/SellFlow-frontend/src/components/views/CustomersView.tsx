import { useQuery } from "@tanstack/react-query";
import { Users, MessageCircle } from "lucide-react";
import { Empty, PageHeader, Panel } from "@/components/app/kit";
import { api } from "@/lib/api";
import { dateTime, naira } from "@/lib/format";


export function CustomersView() {
  const { data = [] } = useQuery({ queryKey: ["customers"], queryFn: api.customers });
  return (
    <>
      <PageHeader title="Customers" subtitle={`${data.length} people have shopped with you on WhatsApp.`} />
      <Panel>
        {!data.length ? <Empty icon={<Users className="size-5" />} title="No customers yet" /> : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[560px] text-sm">
              <thead><tr className="border-b bg-muted/40 text-left text-xs font-bold uppercase tracking-wider text-muted-foreground">
                <th className="px-5 py-3">Customer</th><th className="px-5 py-3 text-right">Orders</th><th className="px-5 py-3 text-right">Total spent</th><th className="px-5 py-3 text-right">Last order</th><th className="px-5 py-3" />
              </tr></thead>
              <tbody className="divide-y">
                {data.map((c) => (
                  <tr key={c.id} className="hover:bg-muted/40">
                    <td className="px-5 py-3.5">
                      <div className="flex items-center gap-3">
                        <span className="grid size-9 place-items-center rounded-full bg-accent text-xs font-bold text-accent-foreground">
                          {(c.fullName ?? "?").split(" ").map((s) => s[0]).slice(0, 2).join("")}
                        </span>
                        <div><p className="font-semibold">{c.fullName ?? "Unnamed"}</p><p className="text-xs text-muted-foreground">+{c.whatsAppNumber}</p></div>
                      </div>
                    </td>
                    <td className="tabular px-5 py-3.5 text-right">{c.orderCount}</td>
                    <td className="tabular px-5 py-3.5 text-right font-bold">{naira(c.totalSpent)}</td>
                    <td className="px-5 py-3.5 text-right text-muted-foreground">{dateTime(c.lastOrderAt)}</td>
                    <td className="px-5 py-3.5 text-right">
                      <a href={`https://wa.me/${c.whatsAppNumber}`} target="_blank" rel="noreferrer" className="inline-flex items-center gap-1 text-xs font-bold text-primary"><MessageCircle className="size-3.5" />Chat</a>
                    </td>
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
