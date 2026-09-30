import { defineConfig } from "vite";
export default defineConfig({
  base: "/",
  build: {
    target: "esnext",
    outDir: "../../Builds/BeaverCreekThree",
    emptyOutDir: true,
  },
});
