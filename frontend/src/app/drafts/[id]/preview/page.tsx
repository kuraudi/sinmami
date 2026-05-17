import { DraftPreviewClient } from "@/features/draft-flow/draft-preview-client";

type Props = {
  params: Promise<{ id: string }>;
};

export default async function DraftPreviewPage({ params }: Props) {
  const { id } = await params;
  return <DraftPreviewClient draftId={id} />;
}
