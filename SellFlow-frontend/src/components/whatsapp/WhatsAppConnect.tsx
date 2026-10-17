import { useQuery } from "@tanstack/react-query";
import { CheckCircle2, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { api } from "@/lib/api";
import { connectSupportLinks } from "@/lib/support";

function SupportButtons({ businessName, label }: { businessName: string; label: string }) {
  const links = connectSupportLinks(businessName);
  if (!links.whatsapp && !links.email) return null;
  return (
    <div className="flex flex-wrap gap-2">
      {links.whatsapp && <Button asChild><a href={links.whatsapp} target="_blank" rel="noreferrer">{label}</a></Button>}
      {links.email && <Button asChild variant="outline"><a href={links.email}>Email us</a></Button>}
    </div>
  );
}

/** Steps for linking a number, with a live "waiting" status that updates on its own. */
export function WhatsAppInstructions({ businessName }: { businessName: string }) {
  // Our team links the number for now, so keep checking until the API reports it as connected.
  const biz = useQuery({ queryKey: ["business"], queryFn: api.business, refetchInterval: 8000, refetchOnWindowFocus: true });
  return (
    <div className="space-y-5">
      <ol className="space-y-3 text-sm">
        <li className="flex gap-3"><span className="font-extrabold text-primary">1</span><span>Choose the number customers should message. You need to be able to receive a verification code on it.</span></li>
        <li className="flex gap-3"><span className="font-extrabold text-primary">2</span><span>Send us that number. Our team connects it to your store.</span></li>
        <li className="flex gap-3"><span className="font-extrabold text-primary">3</span><span>This page updates by itself once the connection is live.</span></li>
      </ol>
      <SupportButtons businessName={businessName} label="Message us on WhatsApp" />
      <div className="flex items-center gap-2 rounded-lg bg-muted/60 px-3 py-2.5 text-sm text-muted-foreground" role="status">
        <Loader2 className="size-4 animate-spin" aria-hidden /> Waiting for the connection…
        <button type="button" onClick={() => void biz.refetch()} className="ml-auto text-xs font-bold text-primary">Check now</button>
      </div>
    </div>
  );
}

/** Dashboard version: shows the connected number, or the steps to connect one. */
export function WhatsAppConnectCard({ businessName }: { businessName: string }) {
  const biz = useQuery({
    queryKey: ["business"],
    queryFn: api.business,
    refetchInterval: (q) => (q.state.data?.whatsAppConnected ? false : 8000),
  });

  if (biz.data?.whatsAppConnected) {
    return (
      <div className="surface space-y-4 p-6">
        <div className="flex items-center gap-2 font-bold text-success"><CheckCircle2 className="size-5" aria-hidden /> Connected</div>
        <div>
          <p className="text-sm text-muted-foreground">Customers message this number</p>
          <p className="tabular text-2xl font-extrabold tracking-tight">{biz.data.whatsAppNumber ?? "Your WhatsApp number"}</p>
        </div>
        <p className="text-sm text-muted-foreground">They get your store menu and can browse products, place orders and send payment receipts.</p>
        <SupportButtons businessName={businessName} label="Need to change your number?" />
      </div>
    );
  }

  return (
    <div className="surface space-y-5 p-6">
      <div>
        <h2 className="text-lg font-extrabold tracking-tight">Connect your WhatsApp number</h2>
        <p className="mt-1 text-sm text-muted-foreground">Until a number is connected, customers can't order from your store on WhatsApp.</p>
      </div>
      <WhatsAppInstructions businessName={businessName} />
    </div>
  );
}
