import { Link, useNavigate } from "@tanstack/react-router";
import { Check, LogOut, MessageCircle } from "lucide-react";
import type { ReactNode } from "react";
import { storage } from "@/lib/api";
import { cn } from "@/lib/utils";

const steps = [
  { key: "business", label: "Your business" },
  { key: "plan", label: "Subscription" },
  { key: "whatsapp", label: "WhatsApp" },
] as const;

export function SetupShell({ current, children }: { current: (typeof steps)[number]["key"]; children: ReactNode }) {
  const navigate = useNavigate();
  const idx = steps.findIndex((s) => s.key === current);
  return (
    <div className="min-h-screen bg-background">
      <header className="mx-auto flex max-w-3xl items-center justify-between px-5 py-5">
        <Link to="/" className="flex items-center gap-2 text-lg font-extrabold">
          <MessageCircle className="size-6 text-primary" aria-hidden /> SellFlow
        </Link>
        <button
          onClick={() => { storage.clear(); navigate({ to: "/login" }); }}
          className="flex items-center gap-1.5 text-sm font-semibold text-muted-foreground hover:text-foreground">
          <LogOut className="size-4" /> Sign out
        </button>
      </header>
      <main className="mx-auto max-w-xl px-5 pb-16">
        <ol className="mb-6 flex items-center gap-2" aria-label="Setup progress">
          {steps.map((s, i) => (
            <li key={s.key} className="flex flex-1 items-center gap-2" aria-current={i === idx ? "step" : undefined}>
              <span className={cn("grid size-7 shrink-0 place-items-center rounded-full text-xs font-bold",
                i < idx ? "bg-primary text-primary-foreground" : i === idx ? "bg-primary/15 text-primary ring-2 ring-primary" : "bg-muted text-muted-foreground")}>
                {i < idx ? <Check className="size-4" /> : i + 1}
              </span>
              <span className={cn("hidden text-sm font-semibold sm:inline", i === idx ? "text-foreground" : "text-muted-foreground")}>{s.label}</span>
              {i < steps.length - 1 && <span className={cn("h-px flex-1", i < idx ? "bg-primary" : "bg-border")} aria-hidden />}
            </li>
          ))}
        </ol>
        {children}
      </main>
    </div>
  );
}
