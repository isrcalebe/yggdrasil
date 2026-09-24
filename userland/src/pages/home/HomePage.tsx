import type { CurrentSession } from "@yggdrasil/lib/api/identity";

/** Placeholder home: the account area arrives later. */
export function HomePage({ session }: { session: CurrentSession | null }) {
  return (
    <main className="mx-auto max-w-md px-6 py-16">
      <title>Yggdrasil ID</title>
      <h1 className="font-heading text-2xl font-semibold">Yggdrasil ID</h1>
      <p className="mt-4">
        {session ? `Signed in as ${session.email}.` : "You are not signed in."}
      </p>
    </main>
  );
}
