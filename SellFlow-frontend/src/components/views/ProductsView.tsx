import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Plus, Package, ImagePlus, Pencil } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { NumberInput } from "@/components/ui/number-input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";
import { Empty, PageHeader, Pill } from "@/components/app/kit";
import { preparePhoto } from "@/lib/photo";
import { api } from "@/lib/api";
import { naira } from "@/lib/format";
import type { Product, ProductRequest } from "@/lib/types";


const blank: ProductRequest = { name: "", description: "", price: 0, stockQuantity: 0, categoryId: null, imageUrl: null, isActive: true };

export function ProductsView() {
  const qc = useQueryClient();
  const { data = [] } = useQuery({ queryKey: ["products"], queryFn: api.products });
  const { data: cats = [] } = useQuery({ queryKey: ["categories"], queryFn: api.categories });
  const [edit, setEdit] = useState<{ id?: string; form: ProductRequest } | null>(null);
  const [file, setFile] = useState<File | null>(null);
  // Small preview so the merchant can see they picked the right photo from their gallery.
  const preview = useMemo(() => (file ? URL.createObjectURL(file) : null), [file]);
  useEffect(() => () => { if (preview) URL.revokeObjectURL(preview); }, [preview]);
  const [filter, setFilter] = useState<string>("all");

  const save = useMutation({
    mutationFn: async () => {
      const p = edit!.id ? await api.updateProduct(edit!.id, edit!.form) : await api.createProduct(edit!.form);
      if (file) await api.uploadImage(p.id, file);
    },
    onSuccess: () => { toast.success("Product saved"); setEdit(null); setFile(null); qc.invalidateQueries({ queryKey: ["products"] }); qc.invalidateQueries({ queryKey: ["overview"] }); },
    onError: (e: Error) => toast.error(e.message),
  });

  const open = (p?: Product) => {
    setFile(null);
    setEdit(p ? { id: p.id, form: { name: p.name, description: p.description, price: p.price, stockQuantity: p.stockQuantity, categoryId: p.categoryId, imageUrl: p.imageUrl, isActive: p.isActive } } : { form: { ...blank } });
  };
  const f = edit?.form;
  const set = (patch: Partial<ProductRequest>) => setEdit((e) => e && { ...e, form: { ...e.form, ...patch } });
  const list = data.filter((p) => filter === "all" || p.categoryId === filter);

  return (
    <>
      <PageHeader title="Products" subtitle="Only active, in-stock products are shown to customers in your WhatsApp bot."
        actions={<Button onClick={() => open()}><Plus /> Add product</Button>} />
      <div className="mb-4 flex gap-1 overflow-x-auto">
        {[{ id: "all", name: "All" }, ...cats].map((c) => (
          <button key={c.id} onClick={() => setFilter(c.id)} className={`whitespace-nowrap rounded-full border px-3 py-1 text-sm font-semibold ${filter === c.id ? "border-primary bg-primary text-primary-foreground" : "bg-card text-muted-foreground hover:bg-muted"}`}>{c.name}</button>
        ))}
      </div>
      {!list.length ? <div className="surface"><Empty icon={<Package className="size-5" />} title="No products yet" body="Add your first product to open your store." /></div> : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {list.map((p) => {
            const left = p.stockQuantity - p.reservedQuantity;
            return (
              <div key={p.id} className={`surface flex gap-4 p-4 ${p.isActive ? "" : "opacity-60"}`}>
                <div className="grid size-20 shrink-0 place-items-center overflow-hidden rounded-lg bg-muted">
                  {p.imageUrl ? <img src={p.imageUrl} alt={p.name} className="size-full object-cover" /> : <Package className="size-6 text-muted-foreground" />}
                </div>
                <div className="min-w-0 flex-1">
                  <div className="flex items-start justify-between gap-2">
                    <p className="line-clamp-2 font-semibold leading-snug">{p.name}</p>
                    <button aria-label="Edit" onClick={() => open(p)} className="rounded-md p-1 text-muted-foreground hover:bg-muted"><Pencil className="size-4" /></button>
                  </div>
                  <p className="tabular mt-1 font-extrabold">{naira(p.price)}</p>
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    {!p.isActive ? <Pill>Hidden</Pill> : left <= 0 ? <Pill tone="danger">Out of stock</Pill> : left <= 5 ? <Pill tone="warning">{left} left</Pill> : <Pill tone="success">{left} in stock</Pill>}
                    {p.reservedQuantity > 0 && <Pill tone="info">{p.reservedQuantity} reserved</Pill>}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <Dialog open={!!edit} onOpenChange={(v) => !v && setEdit(null)}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
          <DialogHeader><DialogTitle>{edit?.id ? "Edit product" : "New product"}</DialogTitle></DialogHeader>
          {f && (
            <form id="pf" onSubmit={(e) => { e.preventDefault(); save.mutate(); }} className="space-y-4">
              <div className="space-y-1.5"><Label>Name</Label><Input required value={f.name} onChange={(e) => set({ name: e.target.value })} /></div>
              <div className="space-y-1.5"><Label>Description</Label><Textarea rows={2} value={f.description ?? ""} onChange={(e) => set({ description: e.target.value })} /></div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5"><Label>Price (₦)</Label><NumberInput key={`price-${edit?.id ?? "new"}`} decimal required startEmpty={!edit?.id} placeholder="0" value={f.price} emptyAs={0} onValueChange={(v) => set({ price: v ?? 0 })} /></div>
                <div className="space-y-1.5"><Label>Stock</Label><NumberInput key={`stock-${edit?.id ?? "new"}`} required startEmpty={!edit?.id} placeholder="0" value={f.stockQuantity} emptyAs={0} onValueChange={(v) => set({ stockQuantity: v ?? 0 })} /></div>
              </div>
              <div className="space-y-1.5">
                <Label>Category</Label>
                <select value={f.categoryId ?? ""} onChange={(e) => set({ categoryId: e.target.value || null })} className="h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm">
                  <option value="">No category</option>
                  {cats.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
                </select>
              </div>
              <label className="flex cursor-pointer items-center gap-3 rounded-lg border border-dashed p-3 text-sm text-muted-foreground hover:bg-muted/50">
                {preview ? <img src={preview} alt="Chosen product photo" className="size-12 rounded-md object-cover" /> : <ImagePlus className="size-5" />}
                {file ? "Change photo" : "Choose a photo from your phone"}
                <input
                  type="file"
                  accept="image/*"
                  className="hidden"
                  onChange={async (e) => {
                    const picked = e.target.files?.[0];
                    e.target.value = ""; // lets the merchant pick the same photo again
                    if (!picked) return;
                    try { setFile(await preparePhoto(picked)); } catch (err) { toast.error((err as Error).message); }
                  }}
                />
              </label>
              <p className="-mt-2 text-xs text-muted-foreground">Pick any photo from your gallery or take a new one. We resize it for WhatsApp automatically.</p>
              <div className="flex items-center justify-between rounded-lg border p-3">
                <div><p className="text-sm font-semibold">Visible to customers</p><p className="text-xs text-muted-foreground">Turn off to hide from the bot.</p></div>
                <Switch checked={f.isActive} onCheckedChange={(v) => set({ isActive: v })} />
              </div>
            </form>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setEdit(null)}>Cancel</Button>
            <Button type="submit" form="pf" disabled={save.isPending}>{save.isPending ? "Saving…" : "Save product"}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
