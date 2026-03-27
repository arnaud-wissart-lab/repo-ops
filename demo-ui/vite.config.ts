import { fileURLToPath, URL } from "node:url";
import tailwindcss from "@tailwindcss/vite";
import react from "@vitejs/plugin-react";
import { defineConfig, loadEnv } from "vite";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const target = env.VITE_DEMO_PROXY_TARGET || "http://127.0.0.1:8080";

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        "@": fileURLToPath(new URL("./src", import.meta.url)),
      },
    },
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
