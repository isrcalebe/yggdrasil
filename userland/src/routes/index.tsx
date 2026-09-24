import { createFileRoute } from "@tanstack/react-router";

import { getCurrentSession } from "@yggdrasil/lib/api/identity";
import { HomePage } from "@yggdrasil/pages/home/HomePage";

export const Route = createFileRoute("/")({
  loader: ({ abortController }) => getCurrentSession(abortController.signal),
  component: function HomeRoute() {
    return <HomePage session={Route.useLoaderData()} />;
  },
});
