import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("@/lib/api", () => ({ api: { business: vi.fn() } }));
import { api } from "@/lib/api";
import { WhatsAppConnectCard } from "./WhatsAppConnect";

const biz = (over: object) => ({ id: "b1", name: "Jerry", whatsAppConnected: false, whatsAppNumber: null, ...over });
const show = () =>
  render(
    <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
      <WhatsAppConnectCard businessName="Jerry" />
    </QueryClientProvider>,
  );

describe("WhatsAppConnectCard", () => {
  beforeEach(() => vi.clearAllMocks());

  it("shows how to connect while no number is linked", async () => {
    vi.mocked(api.business).mockResolvedValue(biz({}) as never);
    show();
    expect(await screen.findByText(/Connect your WhatsApp number/i)).toBeTruthy();
    expect(screen.getByText(/Waiting for the connection/i)).toBeTruthy();
  });

  it("shows the connected number once linked", async () => {
    vi.mocked(api.business).mockResolvedValue(biz({ whatsAppConnected: true, whatsAppNumber: "+234 801 111 2222" }) as never);
    show();
    expect(await screen.findByText("+234 801 111 2222")).toBeTruthy();
    expect(screen.getByText("Connected")).toBeTruthy();
    expect(screen.queryByText(/Waiting for the connection/i)).toBeNull();
  });
});
