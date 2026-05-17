import { DocumentPageClient } from "@/features/document-viewer/document-page-client";

type DocumentPageProps = {
  params: Promise<{ id: string }>;
};

export default async function DocumentPage({ params }: DocumentPageProps) {
  const { id } = await params;
  return <DocumentPageClient documentId={id} />;
}
