import "@yggdrasil/css/index.css";

import { createRouter, RouterProvider } from "@tanstack/react-router";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";

import { RouteError } from "@yggdrasil/components/RouteError";

import { routeTree } from "./routeTree.gen";

const router = createRouter({ routeTree, defaultErrorComponent: RouteError });

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

const root = document.getElementById("root");

if (!root) {
  throw new Error("Missing #root element in index.html.");
}

// Pages restored from the back/forward cache keep stale session state (signed in or out elsewhere): reload them.
window.addEventListener("pageshow", (event) => {
  if (event.persisted)
    window.location.reload();
});

createRoot(root).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);
