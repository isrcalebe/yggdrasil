import { redirect } from "@tanstack/react-router";

import { getCurrentSession } from "./api/identity";
import { isLocalUrl } from "./urls";

export interface ReturnUrlSearch {
  /** Where to go after signing in, e.g. /connect/authorize?client_id=… when a game asked for the sign-in. */
  returnUrl?: string;
}

/**
 * Non-local return URLs are dropped here, before any code can navigate to them. The key must be set even when
 * dropped: route search is merged over the root's raw search, so a missing key would keep the unvalidated value.
 */
export function validateReturnUrlSearch(search: Record<string, unknown>): ReturnUrlSearch {
  return {
    returnUrl: typeof search.returnUrl === "string" && isLocalUrl(search.returnUrl) ? search.returnUrl : undefined,
  };
}

/** Already signed in: skip the form. The return URL can be a server route (/connect/authorize), hence a full reload. */
export async function redirectIfSignedIn(returnUrl: string | undefined): Promise<void> {
  if (await getCurrentSession())
    // eslint-disable-next-line @typescript-eslint/only-throw-error -- TanStack Router redirects by throwing
    throw redirect({ href: returnUrl ?? "/", reloadDocument: true });
}
