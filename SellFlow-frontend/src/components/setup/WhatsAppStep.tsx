import { useNavigate } from "@tanstack/react-router";
import { MessageCircle } from "lucide-react";
import { WhatsAppInstructions } from "@/components/whatsapp/WhatsAppConnect";
import { ALLOW_SKIP_WHATSAPP, skipWhatsapp } from "@/lib/setup";

export function WhatsAppStep({ businessName }: { businessName: string }) {
  const navigate = useNavigate();
  return (
    <div className="surface space-y-5 p-7">
      <div className="grid size-11 place-items-center rounded-lg bg-accent text-accent-foreground"><MessageCircle className="size-5" /></div>
      <div>
        <h1 className="text-xl font-extrabold tracking-tight">Connect your WhatsApp number</h1>
        <p className="mt-1 text-sm text-muted-foreground">This is the number your customers will message to browse and order.</p>
      </div>
      <WhatsAppInstructions businessName={businessName} />
      {ALLOW_SKIP_WHATSAPP && (
        <div>
          <button type="button" onClick={() => { skipWhatsapp(); navigate({ to: "/dashboard" }); }}
            className="block text-sm font-semibold text-muted-foreground underline underline-offset-4 hover:text-foreground">
            I'll do this later and open my dashboard
          </button>
          <p className="mt-1 text-xs text-muted-foreground">You can connect it any time from <strong>WhatsApp</strong> in your dashboard menu.</p>
        </div>
      )}
    </div>
  );
}
