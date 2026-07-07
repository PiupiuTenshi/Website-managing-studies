import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: "http://localhost:5246/api/:path*", // Default dotnet port
      },
      {
        source: "/hubs/:path*",
        destination: "http://localhost:5246/hubs/:path*",
      },
    ];
  },
};

export default nextConfig;
