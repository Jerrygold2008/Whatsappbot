import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { BellRing, Landmark, Truck, Store, Tag, X, CreditCard, Check, MessageCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NumberInput } from "@/components/ui/number-input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { PageHeader, Panel, Pill } from "@/components/app/kit";
import { api } from "@/lib/api";
import { dateTime, naira } from "@/lib/format";
import type { Settings as S } from "@/lib/types";

export const Route = createFileRoute("/_app/settings")({
  component: SettingsPage,
  head: () => ({
    meta: [
      { title: "Settings — SellFlow" },
      { name: "description", content: "Store profile, payments, delivery and owner WhatsApp alerts." },
      { property: "og:title", content: "Settings — SellFlow" },
      { property: "og:description", content: "Store profile, payments, delivery and owner WhatsApp alerts." },
    ],
  }),
});

function Section({ icon, title, desc, children }: { icon: React.ReactNode; title: string; desc: string; children: React.ReactNode }) {
  return (
    <Panel>
      <div className="grid gap-5 p-5 md:grid-cols-[240px_1fr] md:gap-8 md:p-6">
        <div>
          <div className="mb-2 grid size-9 place-items-center rounded-lg bg-accent text-accent-foreground">{icon}</div>
          <h2 className="font-bold">{title}</h2>
          <p className="mt-1 text-sm text-muted-foreground">{desc}</p>
        </div>
        <div className="space-y-4">{children}</div>
      </div>
    </Panel>
  );
}

function SettingsPage() {
  const qc = useQueryClient();
  const { data: biz } = useQuery({ queryKey: ["business"], queryFn: api.business });
  const { data: cats = [] } = useQuery({ queryKey: ["categories"], queryFn: api.categories });
  const { data: sub } = useQuery({ queryKey: ["subscription"], queryFn: api.subscription });
  const { data: plans = [] } = useQuery({ queryKey: ["plans"], queryFn: api.plans });
  const [s, setS] = useState<S | null>(null);
  const [profile, setProfile] = useState({ name: "", description: "" });
  const [cat, setCat] = useState("");

  useEffect(() => {
    if (biz) { setS(biz.settings); setProfile({ name: biz.name, description: biz.description ?? "" }); }
  }, [biz]);

  const done = (msg: string) => () => { toast.success(msg); qc.invalidateQueries({ queryKey: ["business"] }); };
  const err = (e: Error) => toast.error(e.message);
  const saveSettings = useMutation({ mutationFn: () => api.updateSettings(s!), onSuccess: done("Settings saved"), onError: err });
  const saveProfile = useMutation({ mutationFn: () => api.updateBusiness(profile.name, profile.description || null), onSuccess: done("Store profile saved"), onError: err });
  const addCat = useMutation({ mutationFn: () => api.createCategory(cat), onSuccess: () => { setCat(""); qc.invalidateQueries({ queryKey: ["categories"] }); }, onError: err });
  const delCat = useMutation({ mutationFn: (id: string) => api.deleteCategory(id), onSuccess: () => qc.invalidateQueries({ queryKey: ["categories"] }), onError: err });
  const pay = useMutation({ mutationFn: (id: string) => api.pay(id), onSuccess: (r) => { window.location.href = r.paymentUrl; }, onError: err });

  if (!s) return <div className="h-96 animate-pulse rounded-xl bg-muted" />;
  const set = (patch: Partial<S>) => setS({ ...s, ...patch });

  return (
    <>
      <PageHeader title="Settings" subtitle="Configure how your store sells and how you get notified." />
      <div className="space-y-5">
        <Section icon={<BellRing className="size-4" />} title="Owner WhatsApp alerts" desc="Your personal number. Receipts, new orders and stock reminders arrive here with tap-to-act buttons.">
          <div className="space-y-1.5">
            <Label>Owner WhatsApp number</Label>
            <Input placeholder="2348012345678" inputMode="tel" value={s.ownerWhatsAppNumber ?? ""} onChange={(e) => set({ ownerWhatsAppNumber: e.target.value })} />
            <p className="text-xs text-muted-foreground">International format, digits only. Use a different number from your store's WhatsApp line.</p>
          </div>
          <div className="rounded-xl bg-secondary p-4">
            <p className="mb-3 text-xs font-bold uppercase tracking-wider text-muted-foreground">What you'll receive</p>
            <div className="max-w-sm rounded-lg rounded-tl-none bg-card p-3 text-[13px] leading-relaxed shadow-sm">
              <p className="font-bold">🧾 PAYMENT PROOF RECEIVED</p>
              <p className="text-muted-foreground">━━━━━━━━━━━━━━</p>
              <p><b>Order:</b> ORD-480121-261006</p>
              <p><b>Customer:</b> Adaeze Okafor</p>
              <p><b>Total:</b> ₦31,000</p>
              <p className="mt-1 text-muted-foreground">Please verify the credit in your bank app, then choose an option below.</p>
              <div className="mt-2 grid grid-cols-2 gap-px overflow-hidden rounded-md border bg-border text-center text-xs font-semibold text-info">
                <span className="bg-card py-2">✅ Confirm payment</span><span className="bg-card py-2">❌ Reject payment</span>
              </div>
            </div>
            <p className="mt-3 text-xs text-muted-foreground">Order cards follow with <b>Processing</b>, <b>In transit</b> and <b>Delivered</b> buttons. Reply <b>pending</b> anytime to see what needs you, or <b>stock</b> for low-stock items.</p>
          </div>
          <Button onClick={() => saveSettings.mutate()} disabled={saveSettings.isPending}>Save alerts</Button>
        </Section>

        <Section icon={<Landmark className="size-4" />} title="Payments" desc="How customers can pay for orders placed on WhatsApp.">
          <div className="flex items-center justify-between rounded-lg border p-3"><div><p className="text-sm font-semibold">Bank transfer (Pay now)</p><p className="text-xs text-muted-foreground">Customers send a receipt for your approval.</p></div><Switch checked={s.acceptBankTransfer} onCheckedChange={(v) => set({ acceptBankTransfer: v })} /></div>
          {s.acceptBankTransfer && (
            <div className="grid gap-3 sm:grid-cols-3">
              <div className="space-y-1.5"><Label>Bank</Label><Input value={s.bankName ?? ""} onChange={(e) => set({ bankName: e.target.value })} /></div>
              <div className="space-y-1.5"><Label>Account number</Label><Input value={s.accountNumber ?? ""} onChange={(e) => set({ accountNumber: e.target.value })} /></div>
              <div className="space-y-1.5"><Label>Account name</Label><Input value={s.accountName ?? ""} onChange={(e) => set({ accountName: e.target.value })} /></div>
            </div>
          )}
          <div className="flex items-center justify-between rounded-lg border p-3"><div><p className="text-sm font-semibold">Pay on delivery</p><p className="text-xs text-muted-foreground">You accept each order before it's prepared.</p></div><Switch checked={s.acceptPayOnDelivery} onCheckedChange={(v) => set({ acceptPayOnDelivery: v })} /></div>
          {s.acceptPayOnDelivery && (
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="space-y-1.5"><Label>Max order value (₦)</Label><NumberInput decimal placeholder="No limit" value={s.maxPodOrderValue} onValueChange={(v) => set({ maxPodOrderValue: v })} /></div>
              <div className="flex items-center justify-between gap-3 rounded-lg border p-3"><span className="text-sm">Returning customers only</span><Switch checked={s.podRequiresReturningCustomer} onCheckedChange={(v) => set({ podRequiresReturningCustomer: v })} /></div>
            </div>
          )}
          <Button onClick={() => saveSettings.mutate()} disabled={saveSettings.isPending}>Save payments</Button>
        </Section>

        <Section icon={<Truck className="size-4" />} title="Delivery" desc="Flat fee added to every order. Use 0 for free delivery.">
          <div className="max-w-xs space-y-1.5"><Label>Delivery fee (₦)</Label><NumberInput decimal placeholder="0" value={s.deliveryFee} onValueChange={(v) => set({ deliveryFee: v })} /></div>
          <Button onClick={() => saveSettings.mutate()} disabled={saveSettings.isPending}>Save delivery</Button>
        </Section>

        <Section icon={<Store className="size-4" />} title="Store profile" desc="Shown to customers when they message your store.">
          <div className="space-y-1.5"><Label>Business name</Label><Input value={profile.name} onChange={(e) => setProfile({ ...profile, name: e.target.value })} /></div>
          <div className="space-y-1.5"><Label>Description</Label><Textarea rows={2} value={profile.description} onChange={(e) => setProfile({ ...profile, description: e.target.value })} /></div>
          <div className="flex items-center gap-2 text-sm text-muted-foreground"><MessageCircle className="size-4" /> Store line: {biz?.whatsAppNumber ?? "not connected"} {biz?.whatsAppConnected && <Pill tone="success">Connected</Pill>}</div>
          <Button onClick={() => saveProfile.mutate()} disabled={saveProfile.isPending}>Save profile</Button>
        </Section>

        <Section icon={<Tag className="size-4" />} title="Categories" desc="Group products to keep your catalogue tidy.">
          <div className="flex flex-wrap gap-2">
            {cats.map((c) => (
              <span key={c.id} className="inline-flex items-center gap-1 rounded-full border bg-card py-1 pl-3 pr-1.5 text-sm font-medium">
                {c.name}<button aria-label={`Remove ${c.name}`} onClick={() => delCat.mutate(c.id)} className="rounded-full p-0.5 hover:bg-muted"><X className="size-3.5" /></button>
              </span>
            ))}
          </div>
          <form onSubmit={(e) => { e.preventDefault(); if (cat.trim()) addCat.mutate(); }} className="flex max-w-sm gap-2">
            <Input placeholder="New category" value={cat} onChange={(e) => setCat(e.target.value)} /><Button type="submit" variant="soft">Add</Button>
          </form>
        </Section>

        <Section icon={<CreditCard className="size-4" />} title="Subscription" desc="Your store stays live while your plan is active.">
          <div className="flex items-center gap-2 text-sm">
            {sub?.isActive ? <Pill tone="success">Active</Pill> : <Pill tone="danger">Inactive</Pill>}
            <span className="text-muted-foreground">{sub?.planName ?? "No plan"}{sub?.endDate ? ` · renews ${dateTime(sub.endDate)}` : ""}</span>
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            {plans.map((p) => (
              <div key={p.id} className="rounded-xl border p-4">
                <p className="font-bold">{p.name}</p>
                <p className="tabular mt-1 text-2xl font-extrabold">{naira(p.price)}<span className="text-sm font-medium text-muted-foreground"> / {p.durationInDays} days</span></p>
                <Button size="sm" variant={sub?.planName === p.name ? "outline" : "default"} className="mt-3 w-full" onClick={() => pay.mutate(p.id)}>
                  {sub?.planName === p.name ? <><Check /> Renew</> : "Choose plan"}
                </Button>
              </div>
            ))}
          </div>
        </Section>
      </div>
    </>
  );
}
