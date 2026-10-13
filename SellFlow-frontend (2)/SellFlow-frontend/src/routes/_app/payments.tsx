import { createFileRoute } from "@tanstack/react-router";
import { PaymentsView } from "@/components/views/PaymentsView";

export const Route = createFileRoute("/_app/payments")({
  component: PaymentsView,
  head: () => ({
    meta: [
      { title: "Payments — SellFlow" },
      { name: "description", content: "Track every transfer and payment for your store." },
      { property: "og:title", content: "Payments — SellFlow" },
      { property: "og:description", content: "Track every transfer and payment for your store." },
    ],
  }),
});
