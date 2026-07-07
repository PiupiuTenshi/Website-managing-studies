import type { Metadata } from "next";
import { AuthProvider } from "@/features/auth/auth-provider";
import "./globals.css";

export const metadata: Metadata = {
  title: "Remote Assignment Platform",
  description: "Phase 0 project foundation for the remote assignment platform."
};

export default function RootLayout({
  children
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body>
        <AuthProvider>{children}</AuthProvider>
      </body>
    </html>
  );
}
