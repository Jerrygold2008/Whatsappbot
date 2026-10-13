import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useState } from "react";
import { toast } from "sonner";
import { Store } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { api, storage } from "@/lib/api";

export const Route = createFileRoute("/onboarding")({
  component: Onboarding,
  head: () => ({
    meta: [
      { title: "Set up your store — SellFlow" },
      { name: "description", content: "Create your SellFlow business profile." },
      { property: "og:title", content: "Set up your store — SellFlow" },
      { property: "og:description", content: "Create your SellFlow business profile." },
    ],
  }),
});

function Onboarding() {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [desc, setDesc] = useState("");
  const [busy, setBusy] = useState(false);
  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setBusy(true);
    try {
      const res = await api.createBusiness(name, desc);
      storage.setSession(res.token, storage.email() ?? "");
      navigate({ to: "/settings" });
      toast.success("Store created. Next, add your payment and owner WhatsApp details.");
    } catch (err) { toast.error((err as Error).message); } finally { setBusy(false); }
  };
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-5">
      <form onSubmit={submit} className="surface w-full max-w-md space-y-5 p-7">
        <div className="grid size-11 place-items-center rounded-lg bg-accent text-accent-foreground"><Store className="size-5" /></div>
        <div>
          <h1 className="text-xl font-extrabold tracking-tight">Set up your store</h1>
          <p className="mt-1 text-sm text-muted-foreground">This name greets customers in your WhatsApp bot.</p>
        </div>
        <div className="space-y-1.5"><Label>Business name</Label><Input required value={name} onChange={(e) => setName(e.target.value)} /></div>
        <div className="space-y-1.5"><Label>Short description</Label><Textarea rows={3} value={desc} onChange={(e) => setDesc(e.target.value)} /></div>
        <Button type="submit" className="w-full" disabled={busy}>{busy ? "Creating…" : "Create store"}</Button>
      </form>
    </div>
  );
}
