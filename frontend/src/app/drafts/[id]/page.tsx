import { DraftFlowClient } from "@/features/draft-flow/draft-flow-client";

type DraftPageProps = {
  params: Promise<{ id: string }>;
};

export default async function DraftPage({ params }: DraftPageProps) {
  const { id } = await params;
  return <DraftFlowClient draftId={id} />;
}
