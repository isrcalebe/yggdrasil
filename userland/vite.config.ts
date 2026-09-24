import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

const api = "http://localhost:8100";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      "/api": api,
      "/connect": api,
      "/.well-known": api,
    },
  },
  resolve: {
    tsconfigPaths: true,
  },
});
