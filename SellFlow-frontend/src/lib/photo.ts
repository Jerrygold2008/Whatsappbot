// Merchants pick photos straight from their phone gallery. Phone photos are often huge (5 MB or more) and may be
// HEIC or WebP, but WhatsApp can only show JPEG or PNG. So every photo is shrunk and converted to JPEG here,
// in the browser, before upload. The merchant never has to think about size or format.

export const MAX_SIDE = 1600; // pixels on the longest side: plenty for a WhatsApp card, and keeps files small

export function fitWithin(width: number, height: number, max = MAX_SIDE) {
  const scale = Math.min(1, max / Math.max(width, height));
  return { width: Math.max(1, Math.round(width * scale)), height: Math.max(1, Math.round(height * scale)) };
}

export function photoFileName(original: string) {
  const base = original.replace(/\.[^./\\]+$/, "").replace(/[^\w-]+/g, "-").replace(/^-+|-+$/g, "") || "photo";
  return `${base}.jpg`;
}

async function decode(file: File): Promise<{ source: CanvasImageSource; width: number; height: number; close: () => void }> {
  if (typeof createImageBitmap === "function") {
    try {
      const bmp = await createImageBitmap(file, { imageOrientation: "from-image" }); // keeps phone photos upright
      return { source: bmp, width: bmp.width, height: bmp.height, close: () => bmp.close() };
    } catch {
      /* fall back to an <img> element */
    }
  }
  const url = URL.createObjectURL(file);
  try {
    const img = new Image();
    img.src = url;
    await img.decode();
    return { source: img, width: img.naturalWidth, height: img.naturalHeight, close: () => URL.revokeObjectURL(url) };
  } catch (e) {
    URL.revokeObjectURL(url);
    throw e;
  }
}

/** Shrinks the picked photo and converts it to JPEG. Throws a message the merchant can read. */
export async function preparePhoto(file: File): Promise<File> {
  const looksLikeImage = file.type.startsWith("image/") || /\.(jpe?g|png|webp|heic|heif|gif|bmp)$/i.test(file.name);
  if (!looksLikeImage) throw new Error("Please choose a photo.");

  let img: Awaited<ReturnType<typeof decode>>;
  try {
    img = await decode(file);
  } catch {
    throw new Error("We couldn't read that photo. Please pick another one.");
  }

  try {
    const { width, height } = fitWithin(img.width, img.height);
    const canvas = document.createElement("canvas");
    canvas.width = width;
    canvas.height = height;
    const ctx = canvas.getContext("2d");
    if (!ctx) throw new Error("This browser can't prepare photos. Please try another browser.");
    ctx.fillStyle = "#ffffff"; // transparent PNGs get a white background instead of black
    ctx.fillRect(0, 0, width, height);
    ctx.drawImage(img.source, 0, 0, width, height);
    const blob = await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.85));
    if (!blob) throw new Error("We couldn't prepare that photo. Please pick another one.");
    return new File([blob], photoFileName(file.name), { type: "image/jpeg" });
  } finally {
    img.close();
  }
}
