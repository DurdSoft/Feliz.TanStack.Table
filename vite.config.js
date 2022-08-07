import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
    root: "src/Docs",
    build: {
        outDir: "../../public",
        emptyOutDir: true,
        sourcemap: true
    },
    plugins: [
        react()
    ]
});