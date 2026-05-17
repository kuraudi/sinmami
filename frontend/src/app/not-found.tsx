import Link from "next/link";

export default function NotFound() {
  return (
    <main className="subtle-grid min-h-screen px-4 py-6 md:px-8">
      <div className="mx-auto flex max-w-4xl flex-col gap-6">
        <section className="surface-card rounded-4xl px-6 py-8 md:px-8">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--accent-strong)]">
            404
          </p>
          <h1 className="font-editorial mt-3 text-4xl text-[var(--foreground)] md:text-5xl">
            Страница не найдена
          </h1>
          <p className="mt-4 max-w-2xl text-base leading-7 text-[var(--muted)]">
            Возможно, документ был удален, ссылка устарела или пользователь открыл несуществующий маршрут.
          </p>

          <div className="mt-6">
            <Link
              href="/"
              className="inline-flex rounded-full bg-[var(--accent)] px-6 py-3 text-sm font-semibold text-white transition hover:bg-[var(--accent-strong)]"
            >
              Вернуться на dashboard
            </Link>
          </div>
        </section>
      </div>
    </main>
  );
}
