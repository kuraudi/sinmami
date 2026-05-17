import { notFound } from "next/navigation";
import { AppendixFlowClient } from "@/features/appendix-flow/appendix-flow-client";
import { AppendixType } from "@/types/api";

type AppendixFlowPageProps = {
  params: Promise<{ id: string; type: string }>;
};

export default async function AppendixFlowPage({ params }: AppendixFlowPageProps) {
  const { id, type } = await params;
  const appendixType = Number(type);

  if (!Number.isInteger(appendixType) || !(appendixType in AppendixType)) {
    notFound();
  }

  return <AppendixFlowClient documentId={id} appendixType={appendixType as AppendixType} />;
}
