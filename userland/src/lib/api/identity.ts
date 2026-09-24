import { request, resetAntiforgeryToken } from "./client";
import { ApiError } from "./problem-details";

const identity = "/api/v1/identity";

export const IdentityErrorCode = {
  invalidCredentials: "identity.invalid_credentials",
  lockedOut: "identity.locked_out",
  emailTaken: "identity.email_taken",
} as const;

export interface CurrentSession {
  accountId: string;
  email: string;
}

export interface SignInResult {
  /** Where to go next: the validated return URL (e.g. back to /connect/authorize), or the site root. */
  redirectTo: string;
}

export interface RegisterAccountResult {
  accountId: string;
}

export async function signIn(email: string, password: string, returnUrl?: string): Promise<SignInResult> {
  const result = (await request(`${identity}/sessions`,
    { method: "POST",
      body: {
        email,
        password,
        returnUrl,
      },
    },
  )) as SignInResult;

  resetAntiforgeryToken();

  return result;
}

export async function signOut(): Promise<void> {
  await request(`${identity}/sessions/current`, {
    method: "DELETE",
  });

  resetAntiforgeryToken();
}

export async function getCurrentSession(signal?: AbortSignal): Promise<CurrentSession | null> {
  try {
    return (await request(`${identity}/sessions/current`, { signal })) as CurrentSession;
  } catch (error) {
    if (error instanceof ApiError && error.status === 401)
      return null;

    throw error;
  }
}

export async function registerAccount(email: string, password: string): Promise<RegisterAccountResult> {
  return (await request(`${identity}/accounts`, {
    method: "POST",
    body: {
      email,
      password,
    },
  })) as RegisterAccountResult;
}
