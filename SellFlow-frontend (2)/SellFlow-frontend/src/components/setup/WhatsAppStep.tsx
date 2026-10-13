import { useNavigate } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { Loader2, MessageCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { api } from "@/lib/api";
import { ALLOW_SKIP_WHATSAPP, skipWhatsapp } from "@/lib/setup";

const supportWhatsApp = import.meta.env["VITE_SUPPORT_WHATSAPP"] as string | undefined;
const supportEmail = import.meta.env["VITE_SUPPORT_EMAIL"] as string | undefined;

export function WhatsAppStep({ businessName }: { businessName: string }) {
  const navigate = useNavigate();
  // Our team links the number for now, so poll until the API reports it as connected.
  const biz = useQuery({ queryKey: ["business"], queryFn: api.business, refetchInterval: 8000, refetchOnWindowFocus: true });

  const text = `Hello SellFlow, please connect my WhatsApp number to my store "${businessName}".`;
  const waLink = supportWhatsApp ? `https://wa.me/${supportWhatsApp.replace(/\D/g, "")}?text=${encodeURIComponent(text)}` : null;
  const mailLink = supportEmail ? `mailto:${supportEmail}?subject=${encodeURIComponent("Connect my WhatsApp number")}&body=${encodeURIComponent(text)}` : null;

  return (
    <div className="surface space-y-5 p-7">
      <div className="grid size-11 place-items-center rounded-lg bg-accent text-accent-foreground"><MessageCircle className="size-5" /></div>
      <div>
        <h1 className="text-xl font-extrabold tracking-tight">Connect your WhatsApp number</h1>
        <p className="mt-1 text-sm text-muted-foreground">This is the number your customers will message to browse and order.</p>
      </div>

      <ol className="space-y-3 text-sm">
        <li className="flex gap-3"><span className="font-extrabold text-primary">1</span><span>Choose the number customers should message. You need to be able to receive a verification code on it.</span></li>
        <li className="flex gap-3"><span className="font-extrabold text-primary">2</span><span>Send us that number. Our team connects it to your store.</span></li>
        <li className="flex gap-3"><span className="font-extrabold text-primary">3</span><span>This page continues automatically once the connection is live.</span></li>
      </ol>

      {(waLink || mailLink) && (
        <div className="flex flex-wrap gap-2">
          {waLink && <Button asChild><a href={waLink} target="_blank" rel="noreferrer">Message us on WhatsApp</a></Button>}
          {mailLink && <Button asChild variant="outline"><a href={mailLink}>Email us</a></Button>}
        </div>
      )}

      <div className="flex items-center gap-2 rounded-lg bg-muted/60 px-3 py-2.5 text-sm text-muted-foreground" role="status">
        <Loader2 className="size-4 animate-spin" aria-hidden /> Waiting for the connection…
        <button type="button" onClick={() => void biz.refetch()} className="ml-auto text-xs font-bold text-primary">Check now</button>
      </div>

      {ALLOW_SKIP_WHATSAPP && (
        <button type="button" onClick={() => { skipWhatsapp(); navigate({ to: "/dashboard" }); }}
          className="block text-sm font-semibold text-muted-foreground underline underline-offset-4 hover:text-foreground">
          I'll do this later and open my dashboard
        </button>
      )}
    </div>
  );
}
