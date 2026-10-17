import { Link } from "@tanstack/react-router";
import { useQuery } from "@tanstack/react-query";
import { PageHeader } from "@/components/app/kit";
import { WhatsAppConnectCard } from "@/components/whatsapp/WhatsAppConnect";
import { api } from "@/lib/api";

export function WhatsAppView() {
  const biz = useQuery({ queryKey: ["business"], queryFn: api.business });
  return (
    <>
      <PageHeader title="WhatsApp" subtitle="The number your customers message to browse and order." />
      <div className="max-w-2xl space-y-4">
        <WhatsAppConnectCard businessName={biz.data?.name ?? "my store"} />
        <p className="text-sm text-muted-foreground">
          This is your store's line. The personal number that receives order alerts is set separately in{" "}
          <Link to="/settings" className="font-semibold text-primary underline underline-offset-4">Settings</Link>.
        </p>
      </div>
    </>
  );
}
