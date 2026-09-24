import { useActionState, useEffect } from "react";

import { Button } from "@yggdrasil/components/ui/button";
import { Input } from "@yggdrasil/components/ui/input";
import { Label } from "@yggdrasil/components/ui/label";
import { signIn } from "@yggdrasil/lib/api/identity";

import { RootLine } from "./~components/RootLine";
import { destinationOf } from "./destination";
import { describeSignInError, type SignInErrors } from "./sign-in-errors";

interface SignInState {
  /** Kept across attempts: React resets the form after each submission. */
  email: string;
  errors: SignInErrors;
  redirectTo?: string;
}

const root_growth_ms = 500;

export function SignInPage({ returnUrl }: { returnUrl?: string }) {
  const destination = destinationOf(returnUrl);

  const [state, submit, pending] = useActionState(
    async (_: SignInState, form: FormData): Promise<SignInState> => {
      const email = textOf(form, "email");

      try {
        const { redirectTo } = await signIn(email, textOf(form, "password"), returnUrl);

        return { email, errors: {}, redirectTo };
      } catch (error) {
        return { email, errors: describeSignInError(error) };
      }
    },
    { email: "", errors: {} },
  );

  // Signed in: the root grows to the destination, then the page leaves.
  const arriving = state.redirectTo !== undefined;

  useEffect(() => {
    if (state.redirectTo === undefined)
      return;

    const redirectTo = state.redirectTo;
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Full navigation: the destination can be a server route such as /connect/authorize.
    const timeout = window.setTimeout(() => {
      window.location.assign(redirectTo);
    }, reducedMotion ? 0 : root_growth_ms);

    return () => {
      window.clearTimeout(timeout);
    };
  }, [state.redirectTo]);

  const { errors } = state;

  return (
    <main className="grid min-h-dvh grid-rows-[auto_1fr] md:grid-cols-[18rem_1fr] md:grid-rows-1">
      <title>Sign in · Yggdrasil ID</title>

      <aside className="flex items-center gap-6 border-b bg-card px-6 py-5 md:flex-col md:items-start md:justify-between md:gap-10 md:border-r md:border-b-0 md:px-10 md:py-12">
        <p className="font-heading text-base tracking-wide">Yggdrasil ID</p>
        <RootLine destination={destination} arriving={arriving} />
      </aside>

      <section className="flex items-center px-6 py-12 md:px-16">
        <form action={submit} noValidate className="grid w-full max-w-sm gap-6">
          <h1 className="font-heading text-3xl font-semibold">Sign in</h1>

          {errors.message && (
            <p role="alert" className="border-l-2 border-destructive pl-3 text-sm text-destructive">
              {errors.message}
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
              autoComplete="current-password"
              required
              aria-invalid={errors.password !== undefined}
              aria-describedby={errors.password ? "password-error" : undefined}
            />
            {errors.password && <p id="password-error" className="text-sm text-destructive">{errors.password}</p>}
          </div>

          <Button type="submit" disabled={pending || arriving}>
            {pending || arriving ? "Signing in…" : "Sign in"}
          </Button>

          <p className="text-sm text-muted-foreground">
            New to Yggdrasil?
            {" "}
            <a href="/register" className="text-foreground underline underline-offset-4">Create a Yggdrasil ID</a>
          </p>
        </form>
      </section>
    </main>
  );
}

function textOf(form: FormData, name: string): string {
  const value = form.get(name);

  return typeof value === "string" ? value : "";
}
