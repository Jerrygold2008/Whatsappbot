import { describe, expect, it } from "vitest";
import { computeStep } from "./setup";

const base = { hasBusiness: true, subscriptionActive: true, whatsappConnected: true, whatsappSkipped: false };

describe("computeStep", () => {
  it("asks for business details first", () => {
    expect(computeStep({ ...base, hasBusiness: false, subscriptionActive: false, whatsappConnected: false })).toBe("business");
  });
  it("then the subscription", () => {
    expect(computeStep({ ...base, subscriptionActive: false, whatsappConnected: false })).toBe("plan");
  });
  it("then WhatsApp", () => {
    expect(computeStep({ ...base, whatsappConnected: false })).toBe("whatsapp");
  });
  it("lets merchants skip WhatsApp when allowed", () => {
    expect(computeStep({ ...base, whatsappConnected: false, whatsappSkipped: true })).toBe("done");
  });
  it("opens the dashboard when everything is set up", () => {
    expect(computeStep(base)).toBe("done");
  });
  it("never skips the subscription", () => {
    expect(computeStep({ ...base, subscriptionActive: false, whatsappSkipped: true })).toBe("plan");
  });
});
