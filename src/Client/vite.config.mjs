import { defineConfig } from "vite";

export default defineConfig({
  root: "src/Client",
  server: {
    port: 5173,
    strictPort: false,
    proxy: {
      "/api": "http://localhost:5000",
      "/health": "http://localhost:5000"
    }
  },
  build: {
    outDir: "../Server/wwwroot",
    emptyOutDir: false
  }
});
