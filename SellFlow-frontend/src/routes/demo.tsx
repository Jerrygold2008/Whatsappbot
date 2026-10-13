import { createFileRoute, Link } from "@tanstack/react-router";
import { lazy, Suspense, useEffect, useState } from "react";
import { PublicFooter, PublicHeader } from "@/components/landing/PublicHeader";
import { Button } from "@/components/ui/button";
import { OG_IMAGE, SITE_URL } from "@/lib/seo";

const DemoApp = lazy(() => import("@/components/demo/DemoApp").then((m) => ({ default: m.DemoApp })));

const TITLE = "SellFlow demo: try the dashboard with sample orders";
const DESCRIPTION =
  "Explore the SellFlow dashboard with a sample store. Review a payment receipt, move an order to delivered and manage products. No sign-up needed.";

export const Route = createFileRoute("/demo")({
  component: DemoPage,
  head: () => ({
    meta: [
      { title: TITLE },
      { name: "description", content: DESCRIPTION },
      { property: "og:title", content: TITLE },
      { property: "og:description", content: DESCRIPTION },
      { property: "og:url", content: `${SITE_URL}/demo` },
      { property: "og:image", content: OG_IMAGE },
      { name: "twitter:title", content: TITLE },
      { name: "twitter:description", content: DESCRIPTION },
    ],
    links: [{ rel: "canonical", href: `${SITE_URL}/demo` }],
  }),
});

function DemoPage() {
  // The interactive part only runs in the browser. The text around it is server-rendered for Google.
  const [client, setClient] = useState(false);
  useEffect(() => setClient(true), []);

  return (
    <div className="min-h-screen bg-background text-foreground">
      <PublicHeader active="demo" />
      <main className="mx-auto max-w-6xl px-5 py-10 lg:py-14">
        <h1 className="max-w-2xl text-3xl font-extrabold tracking-tight sm:text-4xl">See how you would run your store on SellFlow</h1>
        <p className="mt-4 max-w-2xl text-lg text-muted-foreground">
          This is the real dashboard filled with orders from a made-up beauty shop. Open an order, review a customer's payment receipt,
          confirm it, then move the order to delivered. Customers get a WhatsApp update at each step.
        </p>
        <div className="mt-8">
          {client ? (
            <Suspense fallback={<div className="h-[520px] animate-pulse rounded-2xl bg-muted/60" />}><DemoApp /></Suspense>
          ) : (
            <div className="h-[520px] animate-pulse rounded-2xl bg-muted/60" aria-hidden />
          )}
        </div>
        <div className="mt-10 flex flex-wrap items-center gap-4">
          <Button asChild size="lg"><Link to="/login">Create your store</Link></Button>
          <p className="text-sm text-muted-foreground">Beta access is 200 naira for 30 days.</p>
        </div>
      </main>
      <PublicFooter />
    </div>
  );
}
