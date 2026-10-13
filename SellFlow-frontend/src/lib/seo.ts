// Set VITE_SITE_URL to your live domain, e.g. https://sellflow.ng (no trailing slash).
const raw = import.meta.env["VITE_SITE_URL"] as string | undefined;

export const SITE_URL = (raw ?? "https://www.example.com").replace(/\/$/, "");
export const SITE_NAME = "SellFlow";
export const DEFAULT_TITLE = "SellFlow: sell on WhatsApp and take orders automatically";
export const DEFAULT_DESCRIPTION =
  "Turn your WhatsApp number into a store. Customers browse, order and pay in chat while you manage orders, stock and payments from one dashboard.";
export const OG_IMAGE = `${SITE_URL}/og-image.png`;

// Private pages (login, setup, dashboard) must not appear in Google.
export const noindex = { name: "robots", content: "noindex, nofollow" } as const;
