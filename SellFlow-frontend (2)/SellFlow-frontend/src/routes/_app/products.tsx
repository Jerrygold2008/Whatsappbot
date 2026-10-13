import { createFileRoute } from "@tanstack/react-router";
import { ProductsView } from "@/components/views/ProductsView";

export const Route = createFileRoute("/_app/products")({
  component: ProductsView,
  head: () => ({
    meta: [
      { title: "Products — SellFlow" },
      { name: "description", content: "Manage the products customers can order on WhatsApp." },
      { property: "og:title", content: "Products — SellFlow" },
      { property: "og:description", content: "Manage the products customers can order on WhatsApp." },
    ],
  }),
});
