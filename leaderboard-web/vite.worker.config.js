import { defineConfig } from 'vite'

export default defineConfig({
  build: {
    emptyOutDir: false,
    lib: {
      entry: 'src/site-worker.js',
      formats: ['es'],
      fileName: 'index',
    },
    outDir: 'dist/server',
  },
})
