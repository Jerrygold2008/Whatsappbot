import { Link } from "@tanstack/react-router";
import { MessageCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { faqs, features, steps } from "@/lib/landing-content";
import { focusRing, PublicFooter, PublicHeader } from "./PublicHeader";

const link = focusRing;

function Bubble({ from, children }: { from: "bot" | "customer"; children: React.ReactNode }) {
  const mine = from === "customer";
  return (
    <div className={mine ? "flex justify-end" : "flex justify-start"}>
      <p
        className={
          "max-w-[85%] whitespace-pre-line rounded-2xl px-3.5 py-2 text-[13.5px] leading-snug shadow-sm " +
          (mine ? "rounded-br-sm bg-primary text-primary-foreground" : "rounded-bl-sm bg-card text-card-foreground")
        }
      >
        {children}
      </p>
    </div>
  );
}

export function Landing() {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <PublicHeader />

      <main>
        {/* Hero: the product's own conversation is the first thing people see */}
        <section className="mx-auto grid max-w-6xl items-center gap-12 px-5 py-14 lg:grid-cols-[1.1fr_0.9fr] lg:py-24">
          <div>
            <h1 className="max-w-xl text-4xl font-extrabold leading-[1.1] tracking-tight sm:text-5xl">
              Your WhatsApp number, now a store that takes orders.
            </h1>
            <p className="mt-5 max-w-lg text-lg text-muted-foreground">
              SellFlow answers your customers, shows your products, collects delivery details and tracks every payment, so you stop
              typing the same prices and bank details all day.
            </p>
            <div className="mt-8 flex flex-wrap items-center gap-4">
              <Button asChild size="lg"><Link to="/login">Create your store</Link></Button>
              <Link to="/demo" className={"text-sm font-bold text-primary " + link}>Try the demo</Link>
            </div>
            <p className="mt-4 text-sm text-muted-foreground">Beta access is 200 naira for 30 days.</p>
          </div>

          <figure className="mx-auto w-full max-w-sm">
            <div className="rounded-[28px] border bg-muted p-3 shadow-lg">
              <div className="flex items-center gap-2.5 rounded-t-2xl bg-primary px-4 py-3 text-primary-foreground">
                <span className="grid size-8 place-items-center rounded-full bg-primary-foreground/20 text-sm font-bold" aria-hidden>A</span>
                <div>
                  <p className="text-sm font-bold leading-tight">Ada's Footwear</p>
                  <p className="text-[11px] opacity-80">Replies instantly</p>
                </div>
              </div>
              <div className="space-y-2.5 rounded-b-2xl bg-muted px-3 py-4">
                <Bubble from="customer">Hi</Bubble>
                <Bubble from="bot">{"Welcome to Ada's Footwear!\n\n1. View products\n2. Chat with the business\n3. My orders"}</Bubble>
                <Bubble from="customer">1</Bubble>
                <Bubble from="bot">{"Products\n\n1. Black Sneaker, ₦35,000\n2. Brown Loafer, ₦45,000\n\nReply with the product number."}</Bubble>
                <Bubble from="customer">1</Bubble>
                <Bubble from="bot">{"Black Sneaker\n₦35,000\nIn stock: 12\n\nHow many would you like?"}</Bubble>
              </div>
            </div>
            <figcaption className="mt-3 text-center text-sm text-muted-foreground">
              An example chat. Your customers see your own store name, products and prices.
            </figcaption>
          </figure>
        </section>

        <section id="how-it-works" className="scroll-mt-20 border-t bg-card">
          <div className="mx-auto max-w-6xl px-5 py-16">
            <h2 className="max-w-xl text-3xl font-extrabold tracking-tight">From first message to delivered order</h2>
            <ol className="mt-10 grid gap-10 sm:grid-cols-2 lg:grid-cols-4">
              {steps.map((s, i) => (
                <li key={s.title}>
                  <span className="text-3xl font-extrabold text-primary" aria-hidden>{i + 1}</span>
                  <h3 className="mt-2 text-lg font-bold">{s.title}</h3>
                  <p className="mt-2 text-[15px] leading-relaxed text-muted-foreground">{s.body}</p>
                </li>
              ))}
            </ol>
          </div>
        </section>

        <section className="border-t">
          <div className="mx-auto grid max-w-6xl gap-10 px-5 py-16 lg:grid-cols-[0.8fr_1.2fr]">
            <h2 className="text-3xl font-extrabold tracking-tight">Built for how Nigerian businesses already sell on WhatsApp</h2>
            <dl className="divide-y border-y">
              {features.map((f) => (
                <div key={f.title} className="grid gap-1 py-5 sm:grid-cols-[1fr_1.4fr] sm:gap-6">
                  <dt className="font-bold">{f.title}</dt>
                  <dd className="text-[15px] leading-relaxed text-muted-foreground">{f.body}</dd>
                </div>
              ))}
            </dl>
          </div>
        </section>

        <section id="pricing" className="scroll-mt-20 border-t bg-card">
          <div className="mx-auto max-w-6xl px-5 py-16">
            <h2 className="text-3xl font-extrabold tracking-tight">Pricing</h2>
            <div className="mt-8 max-w-md rounded-xl border bg-background p-6">
              <p className="text-sm font-bold text-muted-foreground">Beta access</p>
              <p className="mt-1 text-5xl font-extrabold tracking-tight">₦200</p>
              <p className="mt-1 text-sm text-muted-foreground">for 30 days</p>
              <p className="mt-4 text-[15px] leading-relaxed">
                Full access to the dashboard and WhatsApp ordering. SellFlow does not take a cut of your sales, because customers pay
                your bank account directly.
              </p>
              <Button asChild className="mt-6 w-full"><Link to="/login">Create your store</Link></Button>
            </div>
          </div>
        </section>

        <section id="faq" className="scroll-mt-20 border-t">
          <div className="mx-auto max-w-3xl px-5 py-16">
            <h2 className="text-3xl font-extrabold tracking-tight">Questions sellers ask</h2>
            <div className="mt-8 divide-y border-y">
              {faqs.map((f) => (
                <details key={f.q} className="group py-4">
                  <summary className={"cursor-pointer list-none font-bold " + link}>
                    <span className="flex items-center justify-between gap-4">
                      {f.q}
                      <span className="text-primary transition-transform group-open:rotate-45" aria-hidden>+</span>
                    </span>
                  </summary>
                  <p className="mt-3 max-w-prose text-[15px] leading-relaxed text-muted-foreground">{f.a}</p>
                </details>
              ))}
            </div>
          </div>
        </section>

        <section className="border-t bg-primary text-primary-foreground">
          <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-6 px-5 py-14">
            <h2 className="max-w-lg text-3xl font-extrabold tracking-tight">Set up your store today and take your first order on WhatsApp.</h2>
            <Button asChild size="lg" variant="secondary"><Link to="/login">Create your store</Link></Button>
          </div>
        </section>
      </main>
      <PublicFooter />
    </div>
  );
}
