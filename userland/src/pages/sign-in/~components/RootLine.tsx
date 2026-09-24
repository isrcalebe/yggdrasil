import { cn } from "@yggdrasil/lib/utils";
import type { Destination } from "@yggdrasil/pages/sign-in/destination";

interface RootLineProps {
  destination: Destination;
  /** Signed in: the root grows all the way to the destination before the page leaves. */
  arriving: boolean;
}

/**
 * The page's signature: a root running from the Yggdrasil ID trunk to where the player is signing in. It is vertical
 * beside the form, and horizontal on narrow screens.
 */
export function RootLine({ destination, arriving }: RootLineProps) {
  return (
    <div className="flex flex-1 items-center gap-4 md:flex-col md:items-start md:self-stretch">
      <div aria-hidden className="relative h-0.5 flex-1 bg-border md:ml-1 md:h-auto md:w-0.5">
        <div
          className={cn(
            "absolute inset-0 origin-left bg-sap transition-transform duration-500 ease-out motion-reduce:transition-none md:origin-top",
            arriving ? "scale-100" : "scale-x-[0.25] md:scale-x-100 md:scale-y-[0.25]",
          )}
        />
      </div>
      <p className="text-sm leading-snug">
        <span className="block text-muted-foreground">Signing in to</span>
        {destination.kind === "game"
          ? (
              <>
                <span className="block font-medium">a game</span>
                <code className="block font-mono text-xs text-muted-foreground">{destination.clientId}</code>
              </>
            )
          : <span className="block font-medium">your account</span>}
      </p>
    </div>
  );
}
