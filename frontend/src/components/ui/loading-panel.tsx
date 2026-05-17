type LoadingPanelProps = {
  title?: string;
  description?: string;
  lines?: number;
  className?: string;
};

export function LoadingPanel({
  title = "Загрузка данных",
  description = "Подготавливаем интерфейс и подтягиваем состояние из backend.",
  lines = 3,
  className = "",
}: LoadingPanelProps) {
  return (
    <section className={`rounded-[1.6rem] border border-[var(--line)] bg-white/70 px-5 py-6 ${className}`}>
      <div className="h-3 w-28 rounded-full bg-[var(--accent-soft)]" />
      <div className="mt-4 h-7 w-56 rounded-full bg-stone-200/90" />
      <div className="mt-3 h-4 w-full max-w-xl rounded-full bg-stone-200/80" />
      <div className="mt-2 h-4 w-full max-w-lg rounded-full bg-stone-200/70" />

      <div className="mt-6 space-y-3">
        {Array.from({ length: lines }).map((_, index) => (
          <div key={index} className="h-16 rounded-[1.25rem] border border-[var(--line)] bg-stone-100/70" />
        ))}
      </div>

      <div className="sr-only">
        <p>{title}</p>
        <p>{description}</p>
      </div>
    </section>
  );
}
