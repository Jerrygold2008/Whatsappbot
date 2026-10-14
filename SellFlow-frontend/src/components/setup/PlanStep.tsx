import { useState, useEffect } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { toast } from "sonner";
import { CreditCard } from "lucide-react";
import { Button } from "@/components/ui/button";
import { api } from "@/lib/api";
import { naira } from "@/lib/format";
import { cn } from "@/lib/utils";

export function PlanStep() {
  // 1. Add a state to track if the component has mounted in the browser
  const [isMounted, setIsMounted] = useState(false);

  const plans = useQuery({ queryKey: ["plans"], queryFn: api.plans });
  // Same key the wizard reads. Polling lets the page move on as soon as the payment is confirmed,
  // even when the customer pays in another tab.
  const sub = useQuery({ queryKey: ["subscription"], queryFn: api.subscription, refetchInterval: 4000, refetchOnWindowFocus: true });
  const [picked, setPicked] = useState<string | null>(null);

  // 2. Set isMounted to true as soon as the browser takes over
  useEffect(() => {
    setIsMounted(true);
  }, []);

  const list = plans.data ?? [];
  const chosen = list.find((p) => p.id === picked) ?? list[0];

  const pay = useMutation({
    mutationFn: (id: string) => api.pay(id),
    onSuccess: (r) => { window.location.href = r.paymentUrl; },
    onError: (e: Error) => toast.error(e.message),
  });

  // 3. Prevent hydration mismatch: render a stable skeleton on the server
  if (!isMounted) {
    return (
      <div className="surface space-y-5 p-7">
        <div className="grid size-11 place-items-center rounded-lg bg-accent text-accent-foreground">
          <CreditCard className="size-5" />
        </div>
        <div>
          <h1 className="text-xl font-extrabold tracking-tight">Choose your plan</h1>
          <p className="mt-1 text-sm text-muted-foreground">Your subscription keeps your WhatsApp store and dashboard active.</p>
        </div>
        <div className="h-24 animate-pulse rounded-lg bg-muted/60" />
        <div className="h-24 animate-pulse rounded-lg bg-muted/60" />
      </div>
    );
  }

  // 4. The actual component logic (only runs in the browser after mounting)
  return (
    <div className="surface space-y-5 p-7">
      <div className="grid size-11 place-items-center rounded-lg bg-accent text-accent-foreground"><CreditCard className="size-5" /></div>
      <div>
        <h1 className="text-xl font-extrabold tracking-tight">Choose your plan</h1>
        <p className="mt-1 text-sm text-muted-foreground">Your subscription keeps your WhatsApp store and dashboard active.</p>
      </div>

      {plans.isLoading && <div className="h-24 animate-pulse rounded-lg bg-muted/60" />}
      {plans.isError && <p className="text-sm text-destructive">{(plans.error as Error).message}</p>}
      {!plans.isLoading && !plans.isError && list.length === 0 && (
        <p className="text-sm text-muted-foreground">No plans are available right now. Please contact support.</p>
      )}

      <div className="space-y-2.5" role="radiogroup" aria-label="Subscription plans">
        {list.map((p) => {
          const on = p.id === chosen?.id;
          return (
            <button key={p.id} type="button" role="radio" aria-checked={on} onClick={() => setPicked(p.id)}
              className={cn("flex w-full items-center justify-between rounded-xl border p-4 text-left transition-colors",
                on ? "border-primary bg-primary/5 ring-1 ring-primary" : "hover:bg-muted/50")}>
              <span>
                <span className="block font-bold">{p.name}</span>
                <span className="text-sm text-muted-foreground">{p.durationInDays} days of access</span>
              </span>
              <span className="tabular text-xl font-extrabold">{naira(p.price)}</span>
            </button>
          );
        })}
      </div>

      <Button className="w-full" disabled={!chosen || pay.isPending} onClick={() => chosen && pay.mutate(chosen.id)}>
        {pay.isPending ? "Opening payment…" : chosen ? `Pay ${naira(chosen.price)}` : "Pay"}
      </Button>
      <p className="text-center text-xs text-muted-foreground">
        You'll pay securely on Paystack. Already paid? This page continues on its own once the payment is confirmed.
      </p>
      <button type="button" onClick={() => void sub.refetch()} className="mx-auto block text-xs font-bold text-primary">
        Check payment status
      </button>
    </div>
  );
}