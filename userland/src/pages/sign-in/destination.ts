/** Where the player is signing in: a game asking through /connect/authorize, or the Yggdrasil ID site itself. */
export type Destination = { kind: "game"; clientId: string } | { kind: "account" };

export function destinationOf(returnUrl: string | undefined): Destination {
  if (returnUrl?.startsWith("/connect/authorize") === true) {
    const clientId = new URL(returnUrl, window.location.origin).searchParams.get("client_id");

    if (clientId)
      return { kind: "game", clientId };
  }

  return { kind: "account" };
}
