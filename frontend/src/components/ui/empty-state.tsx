import type { ReactNode } from "react";

type EmptyStateProps = {
  eyebrow?: string;
  title: string;
  description: string;
  action?: ReactNode;
  className?: string;
};

export function EmptyState({
  eyebrow,
  title,
  description,
  action,
  className = "",
}: EmptyStateProps) {
  return (
    <section
      className={`rounded-[1.6rem] border border-dashed border-[var(--line)] bg-white/60 px-5 py-8 ${className}`}
    >
      {eyebrow ? (
        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
          {eyebrow}
        </p>
      ) : null}
      <h3 className="font-editorial mt-2 text-2xl text-[var(--foreground)]">{title}</h3>
      <p className="mt-3 max-w-2xl text-sm leading-6 text-[var(--muted)]">{description}</p>
      {action ? <div className="mt-5">{action}</div> : null}
    </section>
  );
}
