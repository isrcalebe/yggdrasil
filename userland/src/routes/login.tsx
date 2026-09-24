import { createFileRoute, redirect } from "@tanstack/react-router";

import { getCurrentSession } from "@yggdrasil/lib/api/identity";
import { isLocalUrl } from "@yggdrasil/lib/urls";
import { SignInPage } from "@yggdrasil/pages/sign-in/SignInPage";

interface LoginSearch {
  /** Where to go after signing in, e.g. /connect/authorize?client_id=… when a game asked for the sign-in. */
  returnUrl?: string;
}

export const Route = createFileRoute("/login")({
  // Non-local return URLs are dropped here, before any code can navigate to them. The key must be set even when
  // dropped: route search is merged over the root's raw search, so a missing key would keep the unvalidated value.
  validateSearch: (search: Record<string, unknown>): LoginSearch => ({
    returnUrl: typeof search.returnUrl === "string" && isLocalUrl(search.returnUrl) ? search.returnUrl : undefined,
  }),
  beforeLoad: async ({ search }) => {
    // Already signed in: skip the form. The return URL can be a server route (/connect/authorize), hence a full reload.
    if (await getCurrentSession())
      // eslint-disable-next-line @typescript-eslint/only-throw-error -- TanStack Router redirects by throwing
      throw redirect({ href: search.returnUrl ?? "/", reloadDocument: true });
  },
  component: function LoginRoute() {
    const { returnUrl } = Route.useSearch();

    return <SignInPage returnUrl={returnUrl} />;
  },
});
