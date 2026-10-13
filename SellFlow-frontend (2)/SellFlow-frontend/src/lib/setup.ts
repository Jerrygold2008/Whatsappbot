import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { api, storage } from "./api";

// New merchants can't always get their number linked straight away (it is connected by our team
// for now), so they may continue to the dashboard and finish WhatsApp later.
// Set to false to make WhatsApp a hard requirement before the dashboard opens.
export const ALLOW_SKIP_WHATSAPP = true;

const SKIP_KEY = "sf_skip_whatsapp";
export const whatsappSkipped = () => typeof window !== "undefined" && localStorage.getItem(SKIP_KEY) === "1";
export const skipWhatsapp = () => localStorage.setItem(SKIP_KEY, "1");

export type SetupStep = "business" | "plan" | "whatsapp" | "done";

/** Setup order: business details, then subscription, then WhatsApp, then the dashboard. */
export function computeStep(i: {
  hasBusiness: boolean;
  subscriptionActive: boolean;
  whatsappConnected: boolean;
  whatsappSkipped: boolean;
}): SetupStep {
  if (!i.hasBusiness) return "business";
  if (!i.subscriptionActive) return "plan";
  if (!i.whatsappConnected && !(ALLOW_SKIP_WHATSAPP && i.whatsappSkipped)) return "whatsapp";
  return "done";
}

export function useSetup(enabled: boolean) {
  const [hasBusiness, setHasBusiness] = useState(() => storage.hasBusiness());
  const on = enabled && hasBusiness;
  const biz = useQuery({ queryKey: ["business"], queryFn: api.business, enabled: on });
  const sub = useQuery({ queryKey: ["subscription"], queryFn: api.subscription, enabled: on });

  const loading = !enabled || (hasBusiness && (biz.isLoading || sub.isLoading));
  const error = (biz.error ?? sub.error) as Error | null;
  const step: SetupStep | null = loading || error
    ? null
    : computeStep({
        hasBusiness,
        subscriptionActive: sub.data?.isActive ?? false,
        whatsappConnected: biz.data?.whatsAppConnected ?? false,
        whatsappSkipped: whatsappSkipped(),
      });

  return {
    step, loading, error, business: biz.data, subscription: sub.data,
    refresh: () => setHasBusiness(storage.hasBusiness()),
    retry: () => { void biz.refetch(); void sub.refetch(); },
  };
}
