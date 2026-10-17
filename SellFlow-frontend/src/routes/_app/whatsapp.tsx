import { createFileRoute } from "@tanstack/react-router";
import { WhatsAppView } from "@/components/views/WhatsAppView";

export const Route = createFileRoute("/_app/whatsapp")({
  component: WhatsAppView,
  head: () => ({ meta: [{ title: "WhatsApp — SellFlow" }] }),
});
