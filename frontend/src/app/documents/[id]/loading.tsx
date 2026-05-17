import { LoadingPanel } from "@/components/ui/loading-panel";

export default function DocumentLoading() {
  return (
    <main className="subtle-grid min-h-screen px-4 py-6 md:px-8">
      <div className="mx-auto flex max-w-7xl flex-col gap-6">
        <LoadingPanel
          className="paper-card rounded-4xl"
          title="Загрузка документа"
          description="Подтягиваем текст договора, guide и связанные приложения."
          lines={5}
        />
      </div>
    </main>
  );
}
