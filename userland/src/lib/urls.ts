/**
 * Same rule as the API: a rooted path, not "//host" or "/\host", and no control characters, which browsers strip
 * ("/\t/host" becomes "//host"). Anything else would let a link send players to another site after signing in.
 */
export function isLocalUrl(url: string): boolean {
  return url.startsWith("/")
    && (url.length === 1 || (url[1] !== "/" && url[1] !== "\\"))
    && !hasControlCharacter(url);
}

function hasControlCharacter(value: string): boolean {
  for (let index = 0; index < value.length; index++) {
    const code = value.charCodeAt(index);

    if (code < 0x20 || (code >= 0x7f && code <= 0x9f))
      return true;
  }

  return false;
}
