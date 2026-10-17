import { describe, expect, it } from "vitest";
import { fitWithin, MAX_SIDE, photoFileName } from "./photo";

describe("fitWithin", () => {
  it("shrinks a big landscape phone photo to the maximum side, keeping its shape", () => {
    expect(fitWithin(4000, 3000)).toEqual({ width: MAX_SIDE, height: 1200 });
  });
  it("shrinks a big portrait photo too", () => {
    expect(fitWithin(3000, 4000)).toEqual({ width: 1200, height: MAX_SIDE });
  });
  it("never enlarges a small photo", () => {
    expect(fitWithin(800, 600)).toEqual({ width: 800, height: 600 });
  });
  it("never returns a zero size", () => {
    expect(fitWithin(10000, 1)).toEqual({ width: MAX_SIDE, height: 1 });
  });
});

describe("photoFileName", () => {
  it("always ends in .jpg", () => {
    expect(photoFileName("IMG_2041.HEIC")).toBe("IMG_2041.jpg");
    expect(photoFileName("my shoe photo (1).png")).toBe("my-shoe-photo-1.jpg");
  });
  it("falls back to a plain name", () => {
    expect(photoFileName(".png")).toBe("photo.jpg");
  });
});
