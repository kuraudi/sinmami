import { LoadingPanel } from "@/components/ui/loading-panel";

export default function RootLoading() {
  return (
    <main className="subtle-grid min-h-screen px-4 py-6 md:px-8">
      <div className="mx-auto flex max-w-7xl flex-col gap-6">
        <section className="surface-card rounded-4xl px-5 py-5 md:px-8">
          <div className="h-3 w-32 rounded-full bg-[var(--accent-soft)]" />
          <div className="mt-4 h-12 w-full max-w-2xl rounded-2xl bg-stone-200/80" />
          <div className="mt-3 h-5 w-full max-w-3xl rounded-full bg-stone-200/70" />
        </section>

        <LoadingPanel
          className="paper-card rounded-4xl"
          title="Загрузка интерфейса"
          description="Подготавливаем страницу и загружаем данные."
          lines={4}
        />
      </div>
    </main>
  );
}
