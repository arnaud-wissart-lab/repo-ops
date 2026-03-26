import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const target = env.VITE_DEMO_PROXY_TARGET || "http://127.0.0.1:8080";

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        "/maintenance": {
          target,
          changeOrigin: true,
        },
        "/supervisor": {
          target,
          changeOrigin: true,
        },
        "/deployment": {
          target,
          changeOrigin: true,
        },
      },
    },
  };
});
