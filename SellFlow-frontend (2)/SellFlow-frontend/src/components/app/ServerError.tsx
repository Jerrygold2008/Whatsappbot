import { Button } from "@/components/ui/button";

export function ServerError({ message, onRetry }: { message: string; onRetry: () => void }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-5">
      <div className="surface w-full max-w-sm space-y-4 p-7 text-center">
        <h1 className="text-lg font-extrabold">We couldn't load your store</h1>
        <p className="text-sm text-muted-foreground">{message}</p>
        <Button onClick={onRetry} className="w-full">Try again</Button>
      </div>
    </div>
  );
}
