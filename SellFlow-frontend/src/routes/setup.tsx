import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { ServerError } from "@/components/app/ServerError";
import { BusinessStep } from "@/components/setup/BusinessStep";
import { PlanStep } from "@/components/setup/PlanStep";
import { SetupShell } from "@/components/setup/SetupShell";
import { WhatsAppStep } from "@/components/setup/WhatsAppStep";
import { ApiError, storage } from "@/lib/api";
import { noindex } from "@/lib/seo";
import { useSetup } from "@/lib/setup";

export const Route = createFileRoute("/setup")({
  component: SetupPage,
  head: () => ({ meta: [{ title: "Set up your store — SellFlow" }, noindex] }),
});

function SetupPage() {
  const navigate = useNavigate();
  const [mounted, setMounted] = useState(false);
  useEffect(() => {
    if (!storage.token()) navigate({ to: "/login" });
    else setMounted(true);
  }, [navigate]);
  return mounted ? <Wizard /> : <div className="min-h-screen bg-background" />;
}

function Wizard() {
  const navigate = useNavigate();
  const setup = useSetup(true);
  const expired = setup.error instanceof ApiError && setup.error.status === 401;

  useEffect(() => {
    if (expired) navigate({ to: "/login" });
    else if (setup.step === "done") navigate({ to: "/dashboard", replace: true });
  }, [expired, setup.step, navigate]);

  if (setup.error && !expired) return <ServerError message={setup.error.message} onRetry={setup.retry} />;
  if (!setup.step || setup.step === "done") return <div className="min-h-screen bg-background" />;

  return (
    <SetupShell current={setup.step}>
      {setup.step === "business" && <BusinessStep onCreated={setup.refresh} />}
      {setup.step === "plan" && <PlanStep />}
      {setup.step === "whatsapp" && <WhatsAppStep businessName={setup.business?.name ?? "my store"} />}
    </SetupShell>
  );
}
