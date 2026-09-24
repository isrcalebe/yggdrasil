import { Link, useRouter } from "@tanstack/react-router";
import { useActionState } from "react";

import { Button } from "@yggdrasil/components/ui/button";
import { type CurrentSession, signOut } from "@yggdrasil/lib/api/identity";

/** Placeholder home: the account area arrives later. */
export function HomePage({ session }: { session: CurrentSession | null }) {
  const router = useRouter();

  const [error, signOutAction, signingOut] = useActionState(async (): Promise<string | null> => {
    try {
      await signOut();
      // Reruns the home loader, which now finds no session.
      await router.invalidate();

      return null;
    } catch {
      return "Could not sign you out. Try again.";
    }
  }, null);

  return (
    <main className="mx-auto max-w-md px-6 py-16">
      <title>Yggdrasil ID</title>
      <h1 className="font-heading text-2xl font-semibold">Yggdrasil ID</h1>
      <p className="mt-4">
        {session ? `Signed in as ${session.email}.` : "You are not signed in."}
      </p>

      {session
        ? (
            <form action={signOutAction} className="mt-6 grid gap-3">
              <Button type="submit" variant="outline" disabled={signingOut} className="justify-self-start">
                {signingOut ? "Signing out…" : "Sign out"}
              </Button>
              {error && <p role="alert" className="text-sm text-destructive">{error}</p>}
            </form>
          )
        : <Link to="/login" className="mt-4 inline-block underline underline-offset-4">Sign in</Link>}
    </main>
  );
}
