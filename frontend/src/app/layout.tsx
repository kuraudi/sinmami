import type { Metadata } from "next";
import { StoreProvider } from "@/store/provider";
import "./globals.css";

export const metadata: Metadata = {
  title: "RentGen MVP",
  description: "Генерация документов аренды через диалог с ИИ",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="ru" className="h-full antialiased overflow-x-hidden">
      <body className="min-h-full overflow-x-hidden">
        <StoreProvider>{children}</StoreProvider>
      </body>
    </html>
  );
}
