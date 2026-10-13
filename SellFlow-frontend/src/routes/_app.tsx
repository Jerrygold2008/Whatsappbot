import { createFileRoute, Link, Outlet, useNavigate, useRouterState } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  LayoutDashboard, ShoppingBag, Package, Users, Wallet, Settings, LogOut, Menu, MessageCircle,
} from "lucide-react";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { ServerError } from "@/components/app/ServerError";
import { api, ApiError, storage } from "@/lib/api";
import { useSetup } from "@/lib/setup";
import { cn } from "@/lib/utils";
import { noindex } from "@/lib/seo";

export const Route = createFileRoute("/_app")({
  component: AppLayout,
  head: () => ({ meta: [noindex] }),
});

const nav = [
  { to: "/dashboard", label: "Overview", icon: LayoutDashboard },
  { to: "/orders", label: "Orders", icon: ShoppingBag },
  { to: "/products", label: "Products", icon: Package },
  { to: "/customers", label: "Customers", icon: Users },
  { to: "/payments", label: "Payments", icon: Wallet },
  { to: "/settings", label: "Settings", icon: Settings },
] as const;

function SideNav({ onNavigate }: { onNavigate?: () => void }) {
  const path = useRouterState({ select: (s) => s.location.pathname });
  const navigate = useNavigate();
  const { data: biz } = useQuery({ queryKey: ["business"], queryFn: api.business });
  const { data: orders } = useQuery({ queryKey: ["orders"], queryFn: () => api.orders() });
  const toReview = orders?.filter((o) => o.paymentStatus === "ProofSubmitted" && o.status === "Pending").length ?? 0;

  return (
    <div className="flex h-full flex-col bg-sidebar text-sidebar-foreground">
      <div className="flex items-center gap-2.5 px-5 py-5">
        <div className="bg-brand grid size-9 place-items-center rounded-lg text-primary-foreground">
          <MessageCircle className="size-5" />
        </div>
        <div>
          <p className="text-[15px] font-extrabold tracking-tight text-sidebar-accent-foreground">SellFlow</p>
          <p className="max-w-[150px] truncate text-xs text-sidebar-foreground/70">{biz?.name ?? "Your store"}</p>
        </div>
      </div>
      <nav className="flex-1 space-y-0.5 px-3">
        {nav.map(({ to, label, icon: Icon }) => {
          const active = path.startsWith(to);
          return (
            <Link key={to} to={to} onClick={onNavigate}
              className={cn("flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-semibold transition-colors",
                active ? "bg-sidebar-accent text-sidebar-accent-foreground" : "hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground")}>
              <Icon className={cn("size-[18px]", active && "text-sidebar-primary")} />
              {label}
              {to === "/orders" && toReview > 0 && (
                <span className="ml-auto rounded-full bg-sidebar-primary px-2 text-xs font-bold text-sidebar-primary-foreground">{toReview}</span>
              )}
            </Link>
          );
        })}
      </nav>
      <div className="m-3 rounded-lg border border-sidebar-border p-3 text-xs">
        <div className="flex items-center gap-2 font-semibold text-sidebar-accent-foreground">
          <span className={cn("size-2 rounded-full", biz?.whatsAppConnected ? "bg-sidebar-primary" : "bg-warning")} />
          {biz?.whatsAppConnected ? "WhatsApp connected" : "WhatsApp not connected"}
        </div>
        <p className="mt-1 text-sidebar-foreground/70">{biz?.whatsAppNumber ?? "Contact support to link your number"}</p>
      </div>
      <button
        onClick={() => { storage.clear(); navigate({ to: "/login" }); }}
        className="mx-3 mb-4 flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-semibold hover:bg-sidebar-accent/60">
        <LogOut className="size-[18px]" /> Sign out
      </button>
    </div>
  );
}

function AppLayout() {
  const navigate = useNavigate();
  const [ready, setReady] = useState(false);
  const [open, setOpen] = useState(false);
  const setup = useSetup(ready);
  const expired = setup.error instanceof ApiError && setup.error.status === 401;

  useEffect(() => {
    if (!storage.token()) navigate({ to: "/login" });
    else setReady(true);
  }, [navigate]);

  // The dashboard only opens once setup is complete: business, then subscription, then WhatsApp.
  useEffect(() => {
    if (!ready) return;
    if (expired) navigate({ to: "/login" });
    else if (setup.step && setup.step !== "done") navigate({ to: "/setup" });
  }, [ready, expired, setup.step, navigate]);

  if (setup.error && !expired) return <ServerError message={setup.error.message} onRetry={setup.retry} />;
  if (!ready || setup.step !== "done") return <div className="min-h-screen bg-background" />;

  return (
    <div className="min-h-screen bg-background lg:pl-64">
      <aside className="fixed inset-y-0 left-0 hidden w-64 lg:block"><SideNav /></aside>
      <Sheet open={open} onOpenChange={setOpen}>
        <SheetContent side="left" className="w-72 border-0 p-0">
          <SheetTitle className="sr-only">Navigation</SheetTitle>
          <SideNav onNavigate={() => setOpen(false)} />
        </SheetContent>
      </Sheet>
      <header className="sticky top-0 z-30 flex items-center gap-3 border-b bg-background/90 px-4 py-3 backdrop-blur lg:hidden">
        <button aria-label="Open menu" onClick={() => setOpen(true)} className="rounded-md p-1.5 hover:bg-muted"><Menu className="size-5" /></button>
        <span className="font-extrabold tracking-tight">SellFlow</span>
      </header>
      <main className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8 lg:py-8"><Outlet /></main>
    </div>
  );
}
