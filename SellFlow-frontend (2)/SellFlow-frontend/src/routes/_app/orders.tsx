import { createFileRoute } from "@tanstack/react-router";
import { OrdersView } from "@/components/views/OrdersView";

export const Route = createFileRoute("/_app/orders")({
  component: OrdersView,
  head: () => ({
    meta: [
      { title: "Orders — SellFlow" },
      { name: "description", content: "Review payments and update deliveries for every WhatsApp order." },
      { property: "og:title", content: "Orders — SellFlow" },
      { property: "og:description", content: "Review payments and update deliveries for every WhatsApp order." },
    ],
  }),
});
