import { IdentityErrorCode } from "@yggdrasil/lib/api/identity";
import { ApiError } from "@yggdrasil/lib/api/problem-details";

export interface RegisterErrors {
  /** Shown above the form: errors that are not about one field. */
  message?: string;
  /** The email already has a Yggdrasil ID: the page offers to sign in instead. */
  emailTaken?: boolean;
  email?: string;
  /** Every broken password rule at once, so the player can fix them in one go. */
  password?: string[];
}

export function describeRegisterError(error: unknown): RegisterErrors {
  if (!(error instanceof ApiError))
    return { message: "Something went wrong. Try again." };

  if (error.code === IdentityErrorCode.emailTaken)
    return { emailTaken: true };

  if (error.status === 0)
    return { message: error.message };

  if (error.status === 400 && error.problem.errors) {
    const password = error.fieldErrors("password");

    return {
      email: error.fieldErrors("email")[0],
      password: password.length > 0 ? password : undefined,
    };
  }

  return { message: "Yggdrasil could not create your ID right now. Try again in a moment." };
}
