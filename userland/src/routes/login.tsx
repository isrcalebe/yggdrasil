import { createFileRoute } from "@tanstack/react-router";

import { redirectIfSignedIn, validateReturnUrlSearch } from "@yggdrasil/lib/return-url";
import { SignInPage } from "@yggdrasil/pages/sign-in/SignInPage";

export const Route = createFileRoute("/login")({
  validateSearch: validateReturnUrlSearch,
  beforeLoad: ({ search }) => redirectIfSignedIn(search.returnUrl),
  component: function LoginRoute() {
    const { returnUrl } = Route.useSearch();

    return <SignInPage returnUrl={returnUrl} />;
  },
});
