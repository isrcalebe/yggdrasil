import { IdentityErrorCode } from "@yggdrasil/lib/api/identity";
import { ApiError } from "@yggdrasil/lib/api/problem-details";

export interface SignInErrors {
  /** Shown above the form: errors that are not about one field. */
  message?: string;
  email?: string;
  password?: string;
}

export function describeSignInError(error: unknown): SignInErrors {
  if (!(error instanceof ApiError))
    return { message: "Something went wrong. Try again." };

  if (error.code === IdentityErrorCode.invalidCredentials)
    return { message: "Email or password is incorrect." };

  if (error.code === IdentityErrorCode.lockedOut)
    return { message: "Too many failed attempts. Wait a few minutes before trying again." };

  if (error.status === 0)
    return { message: error.message };

  if (error.status === 400 && error.problem.errors) {
    return error.fieldErrors("returnUrl").length > 0
      ? { message: "This sign-in link is not valid. Start again from your game." }
      : { email: error.fieldErrors("email")[0], password: error.fieldErrors("password")[0] };
  }

  return { message: "Yggdrasil could not sign you in right now. Try again in a moment." };
}
