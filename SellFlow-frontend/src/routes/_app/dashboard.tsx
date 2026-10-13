import { createFileRoute } from "@tanstack/react-router";
import { DashboardView } from "@/components/views/DashboardView";

export const Route = createFileRoute("/_app/dashboard")({
  component: DashboardView,
  head: () => ({
    meta: [
      { title: "Overview — SellFlow" },
      { name: "description", content: "Today's sales, orders and payments at a glance." },
      { property: "og:title", content: "Overview — SellFlow" },
      { property: "og:description", content: "Today's sales, orders and payments at a glance." },
    ],
  }),
});
