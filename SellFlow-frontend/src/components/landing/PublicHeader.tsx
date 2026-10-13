import { Link } from "@tanstack/react-router";
import { MessageCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export const focusRing = "rounded-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2";
const nav = "text-muted-foreground hover:text-foreground " + focusRing;

export function PublicHeader({ active }: { active?: "demo" }) {
  return (
    <header className="sticky top-0 z-10 border-b bg-background/90 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-5">
        <Link to="/" className={"flex items-center gap-2 text-lg font-extrabold " + focusRing}>
          <MessageCircle className="size-6 text-primary" aria-hidden /> SellFlow
        </Link>
        <nav aria-label="Main" className="flex items-center gap-4 text-sm font-semibold sm:gap-5">
          <Link to="/" hash="how-it-works" className={"hidden sm:inline " + nav}>How it works</Link>
          <Link to="/demo" className={cn(active === "demo" ? "text-foreground" : "text-muted-foreground hover:text-foreground", focusRing)}
            aria-current={active === "demo" ? "page" : undefined}>Demo</Link>
          <Link to="/" hash="pricing" className={"hidden sm:inline " + nav}>Pricing</Link>
          <Link to="/" hash="faq" className={"hidden sm:inline " + nav}>FAQ</Link>
          <Link to="/login" className={nav}>Sign in</Link>
          <Button asChild size="sm"><Link to="/login">Create your store</Link></Button>
        </nav>
      </div>
    </header>
  );
}

export function PublicFooter() {
  return (
    <footer className="border-t">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-5 py-8 text-sm text-muted-foreground">
        <p>© {new Date().getFullYear()} SellFlow</p>
        <div className="flex gap-5">
          <Link to="/demo" className={focusRing}>Demo</Link>
          <Link to="/login" className={focusRing}>Sign in</Link>
        </div>
      </div>
    </footer>
  );
}
