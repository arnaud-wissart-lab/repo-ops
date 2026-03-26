import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
export default defineConfig(function (_a) {
    var mode = _a.mode;
    var env = loadEnv(mode, process.cwd(), "");
    var target = env.VITE_DEMO_PROXY_TARGET || "http://127.0.0.1:8080";
    return {
        plugins: [react()],
        server: {
            port: 5173,
            proxy: {
                "/maintenance": {
                    target: target,
                    changeOrigin: true,
                },
                "/supervisor": {
                    target: target,
                    changeOrigin: true,
                },
            },
        },
    };
});
