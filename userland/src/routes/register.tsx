import { createFileRoute } from "@tanstack/react-router";

import { redirectIfSignedIn, validateReturnUrlSearch } from "@yggdrasil/lib/return-url";
import { RegisterPage } from "@yggdrasil/pages/register/RegisterPage";

export const Route = createFileRoute("/register")({
  validateSearch: validateReturnUrlSearch,
  beforeLoad: ({ search }) => redirectIfSignedIn(search.returnUrl),
  component: function RegisterRoute() {
    const { returnUrl } = Route.useSearch();

    return <RegisterPage returnUrl={returnUrl} />;
  },
});
