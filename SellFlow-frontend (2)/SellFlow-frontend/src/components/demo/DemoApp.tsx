import { useEffect, useState } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { FlaskConical } from "lucide-react";
import { CustomersView } from "@/components/views/CustomersView";
import { DashboardView } from "@/components/views/DashboardView";
import { OrdersView } from "@/components/views/OrdersView";
import { PaymentsView } from "@/components/views/PaymentsView";
import { ProductsView } from "@/components/views/ProductsView";
import { setDemoMode } from "@/lib/api";
import { resetDemo } from "@/lib/demo-api";
import { cn } from "@/lib/utils";

const tabs = [
  { key: "overview", label: "Overview" },
  { key: "orders", label: "Orders" },
  { key: "products", label: "Products" },
  { key: "customers", label: "Customers" },
  { key: "payments", label: "Payments" },
] as const;
type Tab = (typeof tabs)[number]["key"];

/** The real dashboard screens running on sample data. Has its own query cache so it never mixes with a signed-in session. */
export function DemoApp() {
  const [client] = useState(() => {
    // Must happen during the first render: child queries start before any effect of this component runs.
    resetDemo();
    setDemoMode(true);
    return new QueryClient({ defaultOptions: { queries: { retry: false, refetchOnWindowFocus: false, staleTime: Infinity } } });
  });
  useEffect(() => () => setDemoMode(false), []);
  const [tab, setTab] = useState<Tab>("overview");

  return (
    <QueryClientProvider client={client}>
      <div className="overflow-hidden rounded-2xl border bg-background shadow-lg">
        <div className="flex items-center gap-2 border-b bg-accent px-4 py-2 text-xs font-medium text-accent-foreground">
          <FlaskConical className="size-3.5 shrink-0" aria-hidden />
          Sample store with made-up customers. Try anything: nothing you do here is saved or sent.
        </div>
        <div className="flex gap-1 overflow-x-auto border-b px-3 py-2" role="tablist" aria-label="Demo sections">
          {tabs.map((t) => (
            <button key={t.key} role="tab" aria-selected={tab === t.key} onClick={() => setTab(t.key)}
              className={cn("whitespace-nowrap rounded-lg px-3.5 py-1.5 text-sm font-semibold transition-colors",
                tab === t.key ? "bg-primary text-primary-foreground" : "text-muted-foreground hover:bg-muted")}>
              {t.label}
            </button>
          ))}
        </div>
        <div className="p-4 sm:p-6" role="tabpanel">
          {tab === "overview" && <DashboardView onGo={(to) => setTab(to === "/orders" ? "orders" : "products")} />}
          {tab === "orders" && <OrdersView />}
          {tab === "products" && <ProductsView />}
          {tab === "customers" && <CustomersView />}
          {tab === "payments" && <PaymentsView />}
        </div>
      </div>
    </QueryClientProvider>
  );
}
