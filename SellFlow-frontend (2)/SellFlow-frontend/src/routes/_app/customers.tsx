import { createFileRoute } from "@tanstack/react-router";
import { CustomersView } from "@/components/views/CustomersView";

export const Route = createFileRoute("/_app/customers")({
  component: CustomersView,
  head: () => ({
    meta: [
      { title: "Customers — SellFlow" },
      { name: "description", content: "Everyone who has shopped with your store on WhatsApp." },
      { property: "og:title", content: "Customers — SellFlow" },
      { property: "og:description", content: "Everyone who has shopped with your store on WhatsApp." },
    ],
  }),
});
