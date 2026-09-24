import { Link } from "@tanstack/react-router";
import { useActionState } from "react";

import { destinationOf } from "@yggdrasil/components/root-line/destination";
import { RootLine } from "@yggdrasil/components/root-line/RootLine";
import { useArrival } from "@yggdrasil/components/root-line/use-arrival";
import { Button } from "@yggdrasil/components/ui/button";
import { Input } from "@yggdrasil/components/ui/input";
import { Label } from "@yggdrasil/components/ui/label";
import { registerAccount, signIn } from "@yggdrasil/lib/api/identity";

import { describeRegisterError, type RegisterErrors } from "./register-errors";

interface RegisterState {
  /** Kept across attempts: React resets the form after each submission. */
  email: string;
  errors: RegisterErrors;
  redirectTo?: string;
  /** The ID exists but signing in failed: the player continues from the sign-in page. */
  created?: boolean;
}

export function RegisterPage({ returnUrl }: { returnUrl?: string }) {
  const destination = destinationOf(returnUrl);

  const [state, submit, pending] = useActionState(
    async (_: RegisterState, form: FormData): Promise<RegisterState> => {
      const email = textOf(form, "email");
      const password = textOf(form, "password");

      // Two steps, two try blocks: once the ID exists, retrying the form would only report the email as taken.
      try {
        await registerAccount(email, password);
      } catch (error) {
        return { email, errors: describeRegisterError(error) };
      }

      try {
        const { redirectTo } = await signIn(email, password, returnUrl);

        return { email, errors: {}, redirectTo };
      } catch {
        return { email, errors: {}, created: true };
      }
    },
    { email: "", errors: {} },
  );

  // Created and signed in: the root grows to the destination, then the page leaves.
  const arriving = state.redirectTo !== undefined;

  useArrival(state.redirectTo);

  const { errors } = state;

  return (
    <main className="grid min-h-dvh grid-rows-[auto_1fr] md:grid-cols-[18rem_1fr] md:grid-rows-1">
      <title>Create your ID · Yggdrasil ID</title>

      <aside className="flex items-center gap-6 border-b bg-card px-6 py-5 md:flex-col md:items-start md:justify-between md:gap-10 md:border-r md:border-b-0 md:px-10 md:py-12">
        <p className="font-heading text-base tracking-wide">Yggdrasil ID</p>
        <RootLine destination={destination} arriving={arriving} />
      </aside>

      <section className="flex items-center px-6 py-12 md:px-16">
        {state.created
          ? (
              <div className="grid w-full max-w-sm gap-4">
                <h1 className="font-heading text-3xl font-semibold">Your Yggdrasil ID is ready</h1>
                <p>We could not sign you in automatically.</p>
                <Link to="/login" search={{ returnUrl }} className="text-foreground underline underline-offset-4">
                  Sign in to continue
                </Link>
              </div>
            )
          : (
              <form action={submit} noValidate className="grid w-full max-w-sm gap-6">
                <h1 className="font-heading text-3xl font-semibold">Create your Yggdrasil ID</h1>

                {errors.message && (
                  <p role="alert" className="border-l-2 border-destructive pl-3 text-sm text-destructive">
                    {errors.message}
                  </p>
                )}

                {errors.emailTaken && (
                  <p role="alert" className="border-l-2 border-destructive pl-3 text-sm text-destructive">
                    This email already has a Yggdrasil ID.
                    {" "}
                    <Link to="/login" search={{ returnUrl }} className="text-foreground underline underline-offset-4">
                      Sign in instead
                    </Link>
                  </p>
                )}

                <div className="grid gap-2">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                    defaultValue={state.email}
                    aria-invalid={errors.email !== undefined}
                    aria-describedby={errors.email ? "email-error" : undefined}
                  />
                  {errors.email && <p id="email-error" className="text-sm text-destructive">{errors.email}</p>}
                </div>

                <div className="grid gap-2">
                  <Label htmlFor="password">Password</Label>
                  <Input
                    id="password"
                    name="password"
                    type="password"
                    autoComplete="new-password"
                    required
                    aria-invalid={errors.password !== undefined}
                    aria-describedby={errors.password ? "password-hint password-error" : "password-hint"}
                  />
                  {/* Mirrors the password options in IdentityModule.cs: update both together. */}
                  <p id="password-hint" className="text-sm text-muted-foreground">
                    At least 8 characters, with an uppercase letter, a lowercase letter, a number and a symbol.
                  </p>
                  {errors.password && (
                    <ul id="password-error" className="grid gap-1 text-sm text-destructive">
                      {errors.password.map(message => <li key={message}>{message}</li>)}
                    </ul>
                  )}
                </div>

                <Button type="submit" disabled={pending || arriving}>
                  {pending || arriving ? "Creating…" : "Create Yggdrasil ID"}
                </Button>

                <p className="text-sm text-muted-foreground">
                  Already have a Yggdrasil ID?
                  {" "}
                  <Link to="/login" search={{ returnUrl }} className="text-foreground underline underline-offset-4">
                    Sign in
                  </Link>
                </p>
              </form>
            )}
      </section>
    </main>
  );
}

function textOf(form: FormData, name: string): string {
  const value = form.get(name);

  return typeof value === "string" ? value : "";
}
