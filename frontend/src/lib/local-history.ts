const KEY = "rentgen_document_ids";

export function getLocalDocumentIds(): string[] {
  if (typeof window === "undefined") return [];
  try {
    return JSON.parse(localStorage.getItem(KEY) ?? "[]");
  } catch {
    return [];
  }
}

export function addLocalDocumentId(documentId: string) {
  if (typeof window === "undefined") return;
  const ids = getLocalDocumentIds();
  if (!ids.includes(documentId)) {
    localStorage.setItem(KEY, JSON.stringify([documentId, ...ids]));
  }
}
