import { type ErrorComponentProps, useRouter } from "@tanstack/react-router";

import { Button } from "@yggdrasil/components/ui/button";
import { ApiError } from "@yggdrasil/lib/api/problem-details";

/** Shown when a route fails to load, e.g. the API is down or unreachable. */
export function RouteError({ error }: ErrorComponentProps) {
  const router = useRouter();
  const unavailable = error instanceof ApiError && (error.status === 0 || error.status >= 500);

  return (
    <main className="mx-auto max-w-md px-6 py-16">
      <title>Yggdrasil ID</title>
      <h1 className="font-heading text-2xl font-semibold">
        {unavailable ? "Yggdrasil is not responding" : "This page could not load"}
      </h1>
      <p className="mt-4 text-muted-foreground">
        {unavailable ? "Try again in a moment." : "Reload the page to try again."}
      </p>
      <Button className="mt-6" onClick={() => void router.invalidate()}>
        Try again
      </Button>
    </main>
  );
}
