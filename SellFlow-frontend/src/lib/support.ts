const whatsapp = import.meta.env["VITE_SUPPORT_WHATSAPP"] as string | undefined;
const email = import.meta.env["VITE_SUPPORT_EMAIL"] as string | undefined;

/** Ready-made "connect my number" message links, only for the contact options you configured. */
export function connectSupportLinks(businessName: string) {
  const text = `Hello SellFlow, please connect my WhatsApp number to my store "${businessName}".`;
  return {
    whatsapp: whatsapp ? `https://wa.me/${whatsapp.replace(/\D/g, "")}?text=${encodeURIComponent(text)}` : null,
    email: email ? `mailto:${email}?subject=${encodeURIComponent("Connect my WhatsApp number")}&body=${encodeURIComponent(text)}` : null,
  };
}
