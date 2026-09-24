/** RFC 9457 problem details: the body of every error returned by the Yggdrasil API. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** Stable, machine-readable error code, e.g. `identity.invalid_credentials`. */
  code?: string;
  /** Validation errors keyed by camelCase field name. */
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(
      problem.detail
      ?? problem.title
      ?? `Request failed with status ${String(status)}`,
    );

    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }

  get code(): string | undefined {
    return this.problem.code;
  }

  fieldErrors(field: string): string[] {
    return this.problem.errors?.[field] ?? [];
  }
}
