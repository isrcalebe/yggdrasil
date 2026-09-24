import { useEffect } from "react";

/** Matches RootLine's `duration-500`: the page leaves once the root has grown. */
const root_growth_ms = 500;

/**
 * Leaves for `redirectTo` once it is set, after the root has grown to the destination. A full navigation that replaces
 * the current entry: the destination can be a server route such as /connect/authorize, and Back must not return to a
 * form the player already used.
 */
export function useArrival(redirectTo: string | undefined): void {
  useEffect(() => {
    if (redirectTo === undefined)
      return;

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    const timeout = window.setTimeout(() => {
      window.location.replace(redirectTo);
    }, reducedMotion ? 0 : root_growth_ms);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [redirectTo]);
}
