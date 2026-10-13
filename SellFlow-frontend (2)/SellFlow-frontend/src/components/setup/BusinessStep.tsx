import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Store } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { api, storage } from "@/lib/api";

export function BusinessStep({ onCreated }: { onCreated: () => void }) {
  const qc = useQueryClient();
  const [name, setName] = useState("");
  const [desc, setDesc] = useState("");

  const create = useMutation({
    mutationFn: () => api.createBusiness(name.trim(), desc.trim()),
    onSuccess: (res) => {
      // The API returns a fresh token that now carries the business.
      storage.setSession(res.token, storage.email() ?? "", true);
      void qc.invalidateQueries();
      onCreated();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <form onSubmit={(e) => { e.preventDefault(); create.mutate(); }} className="surface space-y-5 p-7">
      <div className="grid size-11 place-items-center rounded-lg bg-accent text-accent-foreground"><Store className="size-5" /></div>
      <div>
        <h1 className="text-xl font-extrabold tracking-tight">Tell us about your business</h1>
        <p className="mt-1 text-sm text-muted-foreground">Customers see this name when they message your WhatsApp store.</p>
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="biz-name">Business name</Label>
        <Input id="biz-name" required maxLength={80} value={name} onChange={(e) => setName(e.target.value)} />
      </div>
      <div className="space-y-1.5">
        <Label htmlFor="biz-desc">Short description</Label>
        <Textarea id="biz-desc" rows={3} maxLength={300} value={desc} onChange={(e) => setDesc(e.target.value)}
          placeholder="What do you sell, and where do you deliver?" />
      </div>
      <Button type="submit" className="w-full" disabled={create.isPending || !name.trim()}>
        {create.isPending ? "Saving…" : "Continue"}
      </Button>
    </form>
  );
}
