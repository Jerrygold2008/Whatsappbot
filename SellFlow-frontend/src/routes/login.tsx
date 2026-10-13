import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { MessageCircle, CheckCircle2, Server } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { api, storage } from "@/lib/api";

export const Route = createFileRoute("/login")({
  component: Login,
  head: () => ({
    meta: [
      { title: "Sign in — SellFlow" },
      { name: "description", content: "Sign in to your SellFlow store dashboard." },
      { property: "og:title", content: "Sign in — SellFlow" },
      { property: "og:description", content: "Sign in to your SellFlow store dashboard." },
    ],
  }),
});

function Login() {
  const navigate = useNavigate();
  const [mode, setMode] = useState<"login" | "register">("login");
  const [apiUrl, setApiUrl] = useState("");
  const [f, setF] = useState({ firstName: "", lastName: "", email: "", password: "" });
  const [busy, setBusy] = useState(false);
  useEffect(() => setApiUrl(storage.apiUrl()), []);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    storage.setApiUrl(apiUrl.trim());
    setBusy(true);
    try {
      const res = mode === "login" ? await api.login(f.email, f.password) : await api.register(f);
      storage.setSession(res.token, res.email);
      navigate({ to: res.businessId ? "/dashboard" : "/onboarding" });
    } catch (err) {
      toast.error((err as Error).message || "Could not reach the server.");
    } finally { setBusy(false); }
  };
  const set = (k: keyof typeof f) => (e: React.ChangeEvent<HTMLInputElement>) => setF({ ...f, [k]: e.target.value });

  return (
    <div className="grid min-h-screen lg:grid-cols-[1.05fr_1fr]">
      <div className="bg-brand relative hidden flex-col justify-between p-12 text-primary-foreground lg:flex">
        <div className="flex items-center gap-2.5 text-lg font-extrabold"><MessageCircle className="size-6" /> SellFlow</div>
        <div className="max-w-md">
          <h2 className="text-4xl font-extrabold leading-tight tracking-tight">Run your whole store from WhatsApp.</h2>
          <ul className="mt-8 space-y-4 text-[15px] text-primary-foreground/90">
            {["Customers browse and order through your store bot", "Approve payment receipts with one tap on your phone", "Update deliveries and keep customers informed automatically"].map((t) => (
              <li key={t} className="flex gap-3"><CheckCircle2 className="mt-0.5 size-5 shrink-0" />{t}</li>
            ))}
          </ul>
        </div>
        <p className="text-xs text-primary-foreground/70">© {new Date().getFullYear()} SellFlow</p>
      </div>
      <div className="flex items-center justify-center px-5 py-12">
        <form onSubmit={submit} className="w-full max-w-sm space-y-5">
          <div>
            <div className="mb-6 flex items-center gap-2 text-lg font-extrabold lg:hidden"><MessageCircle className="size-6 text-primary" /> SellFlow</div>
            <h1 className="text-2xl font-extrabold tracking-tight">{mode === "login" ? "Welcome back" : "Create your account"}</h1>
            <p className="mt-1 text-sm text-muted-foreground">{mode === "login" ? "Sign in to manage your store." : "Start selling on WhatsApp in minutes."}</p>
          </div>
          {mode === "register" && (
            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5"><Label>First name</Label><Input required value={f.firstName} onChange={set("firstName")} /></div>
              <div className="space-y-1.5"><Label>Last name</Label><Input required value={f.lastName} onChange={set("lastName")} /></div>
            </div>
          )}
          <div className="space-y-1.5"><Label>Email</Label><Input type="email" required value={f.email} onChange={set("email")} /></div>
          <div className="space-y-1.5"><Label>Password</Label><Input type="password" required minLength={6} value={f.password} onChange={set("password")} /></div>
          <div className="space-y-1.5 rounded-lg border bg-muted/50 p-3">
            <Label className="flex items-center gap-1.5 text-xs"><Server className="size-3.5" /> API address</Label>
            <Input placeholder="https://api.yourstore.com" value={apiUrl} onChange={(e) => setApiUrl(e.target.value)} className="bg-card" />
            <p className="text-xs text-muted-foreground">Leave empty to explore with sample data.</p>
          </div>
          <Button type="submit" className="h-10 w-full" disabled={busy}>{busy ? "Please wait…" : mode === "login" ? "Sign in" : "Create account"}</Button>
          <p className="text-center text-sm text-muted-foreground">
            {mode === "login" ? "New to SellFlow?" : "Already have an account?"}{" "}
            <button type="button" className="font-bold text-primary" onClick={() => setMode(mode === "login" ? "register" : "login")}>
              {mode === "login" ? "Create an account" : "Sign in"}
            </button>
          </p>
        </form>
      </div>
    </div>
  );
}
