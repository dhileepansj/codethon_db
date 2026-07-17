import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react-swc";
import path from "path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "VITE_");
  return {
    base: env.VITE_APP_BASEPATH || "/novaccodelab",
    server: {
      host: "::",
      port: parseInt(env.VITE_DEV_PORT || "3000"),
      proxy: {
        "/hackathonapi": {
          target: env.VITE_API_BASE_URL || "https://localhost:7100",
          changeOrigin: true,
        },
      },
    },
    plugins: [react()],
    build: {
      chunkSizeWarningLimit: 450,
      rollupOptions: {
        output: {
          manualChunks: {
            vendor: ["react", "react-dom", "react-router-dom"],
            redux: ["@reduxjs/toolkit", "react-redux"],
            editor: ["monaco-editor", "@monaco-editor/react"],
            ui: [
              "@radix-ui/react-dialog",
              "@radix-ui/react-dropdown-menu",
              "@radix-ui/react-tabs",
              "@radix-ui/react-tooltip",
              "@radix-ui/react-select",
              "@radix-ui/react-accordion",
              "@radix-ui/react-scroll-area",
            ],
            utils: ["axios", "date-fns", "clsx", "tailwind-merge", "lucide-react"],
          },
        },
      },
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
  };
});
