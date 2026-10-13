import { useState } from "react";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { cleanNumber, NumberInput } from "./number-input";

function Form({ decimal = false }: { decimal?: boolean }) {
  const [v, setV] = useState(0);
  return (
    <>
      <NumberInput aria-label="n" decimal={decimal} startEmpty emptyAs={0} value={v} onValueChange={(x) => setV(x ?? 0)} />
      <output data-testid="stored">{v}</output>
    </>
  );
}
const box = () => screen.getByLabelText("n") as HTMLInputElement;
const type = (s: string) => fireEvent.change(box(), { target: { value: s } });

describe("NumberInput", () => {
  it("starts empty instead of showing 0", () => {
    render(<Form />);
    expect(box().value).toBe("");
  });
  it("lets you delete everything without snapping back to 0", () => {
    render(<Form />);
    type("25");
    expect(box().value).toBe("25");
    type("2");
    type("");
    expect(box().value).toBe("");
    expect(screen.getByTestId("stored").textContent).toBe("0");
  });
  it("has no spin arrows (plain text field)", () => {
    render(<Form />);
    expect(box().type).toBe("text");
  });
  it("ignores letters and drops leading zeros", () => {
    expect(cleanNumber("12ab3", false)).toBe("123");
    expect(cleanNumber("007", false)).toBe("7");
    expect(cleanNumber("0", false)).toBe("0");
  });
  it("allows two decimals for prices only", () => {
    expect(cleanNumber("1500.759", true)).toBe("1500.75");
    expect(cleanNumber("1.2.3", true)).toBe("1.23");
    expect(cleanNumber("1500.5", false)).toBe("15005");
  });
});
