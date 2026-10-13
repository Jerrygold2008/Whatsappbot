import * as React from "react";
import { Input } from "@/components/ui/input";

/** Keeps only digits (and, for prices, one dot with up to 2 decimals). Drops leading zeros. */
export function cleanNumber(raw: string, decimal: boolean): string {
  const strip = (s: string) => s.replace(/^0+(?=\d)/, "").slice(0, 12);
  if (!decimal) return strip(raw.replace(/\D/g, ""));
  const [whole = "", ...rest] = raw.replace(/[^\d.]/g, "").split(".");
  return rest.length ? `${strip(whole)}.${rest.join("").slice(0, 2)}` : strip(whole);
}

type Props = Omit<React.ComponentProps<"input">, "value" | "defaultValue" | "onChange" | "type" | "inputMode"> & {
  value: number | null;
  onValueChange: (value: number | null) => void;
  /** What to report while the box is empty (default null). */
  emptyAs?: number | null;
  /** Show an empty box even when value is 0, for brand-new forms. */
  startEmpty?: boolean;
  /** Allow up to 2 decimal places, for prices. */
  decimal?: boolean;
};

/**
 * A number field with no up/down arrows whose contents can be fully deleted.
 * A plain `type="number"` input that stores `+value` snaps an empty box back to 0.
 */
export function NumberInput({ value, onValueChange, emptyAs = null, startEmpty = false, decimal = false, ...rest }: Props) {
  const [text, setText] = React.useState(() => (value == null || (startEmpty && value === 0) ? "" : String(value)));
  const emitted = React.useRef<number | null>(value);

  // If the value is changed from outside (for example settings finish loading), show it.
  React.useEffect(() => {
    if (value !== emitted.current) {
      emitted.current = value;
      setText(value == null ? "" : String(value));
    }
  }, [value]);

  return (
    <Input
      {...rest}
      type="text"
      inputMode={decimal ? "decimal" : "numeric"}
      autoComplete="off"
      value={text}
      onChange={(e) => {
        const next = cleanNumber(e.target.value, decimal);
        setText(next);
        const v = next === "" || next === "." ? emptyAs : Number(next);
        emitted.current = v;
        onValueChange(v);
      }}
    />
  );
}
