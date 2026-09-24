import { ApiError, type ProblemDetails } from "./problem-details";

type Method = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface RequestOptions {
  method?: Method;
  body?: unknown;
  signal?: AbortSignal;
}

const antiforgery_path = "/api/v1/identity/antiforgery";
const antiforgery_header = "X-XSRF-TOKEN";
const invalid_antiforgery_token = "antiforgery.invalid_token";
const state_changing_methods = new Set<Method>(["POST", "PUT", "PATCH", "DELETE"]);

let antiforgeryToken: string | undefined;

/** The antiforgery token is bound to the signed-in user: drop it whenever the session changes. */
export function resetAntiforgeryToken(): void {
  antiforgeryToken = undefined;
}

/**
 * Calls the Yggdrasil API on the same origin (the Vite proxy in development, the host in production).
 * State-changing requests carry an antiforgery token; failures throw an {@link ApiError} built from the problem details.
 */
export async function request(path: string, options: RequestOptions = {}): Promise<unknown> {
  if (!state_changing_methods.has(options.method ?? "GET"))
    return send(path, options);

  try {
    return await send(path, options, await getAntiforgeryToken());
  } catch (error) {
    // A token issued for another session (e.g. before signing in, in another tab) is rejected: refresh it once.
    if (!(error instanceof ApiError) || error.code !== invalid_antiforgery_token)
      throw error;

    resetAntiforgeryToken();

    return send(path, options, await getAntiforgeryToken());
  }
}

async function getAntiforgeryToken(): Promise<string> {
  antiforgeryToken ??= ((await send(antiforgery_path, {})) as { token: string }).token;

  return antiforgeryToken;
}

async function send(path: string, options: RequestOptions, token?: string): Promise<unknown> {
  const headers = new Headers({ Accept: "application/json" });

  if (options.body !== undefined)
    headers.set("Content-Type", "application/json");

  if (token !== undefined)
    headers.set(antiforgery_header, token);

  let response: Response;

  try {
    response = await fetch(path, {
      method: options.method ?? "GET",
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      credentials: "same-origin",
      signal: options.signal,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError")
      throw error;

    throw new ApiError(0, {
      title: "Could not reach Yggdrasil. Check your connection and try again.",
    });
  }

  if (!response.ok)
    throw new ApiError(response.status, await readProblem(response));

  return response.status === 204 ? undefined : await response.json();
}

async function readProblem(response: Response): Promise<ProblemDetails> {
  if (response.headers.get("Content-Type")?.includes("json") === true)
    return (await response.json()) as ProblemDetails;

  return {
    status: response.status,
    title: response.statusText,
  };
}
