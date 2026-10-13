import { createFileRoute, redirect } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  beforeLoad: () => { throw redirect({ to: "/dashboard" }); },
  head: () => ({
    meta: [
      { title: "SellFlow — WhatsApp Commerce Dashboard" },
      { name: "description", content: "Manage WhatsApp orders, payments, products and deliveries in one place." },
      { property: "og:title", content: "SellFlow — WhatsApp Commerce Dashboard" },
      { property: "og:description", content: "Manage WhatsApp orders, payments, products and deliveries in one place." },
    ],
  }),
});
