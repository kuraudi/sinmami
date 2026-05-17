import { LoadingPanel } from "@/components/ui/loading-panel";

export default function AppendixFlowLoading() {
  return (
    <div className="px-4 py-6 md:px-8">
      <LoadingPanel
        className="mx-auto max-w-4xl rounded-4xl"
        title="Загрузка сценария приложения"
        description="Подтягиваем шаги мини-опроса и уже известные данные по договору."
        lines={5}
      />
    </div>
  );
}
