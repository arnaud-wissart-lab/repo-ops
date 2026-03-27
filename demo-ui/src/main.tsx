import React from "react";
import ReactDOM from "react-dom/client";
import { ThemeProvider } from "next-themes";
import App from "./App";
import "./styles.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ThemeProvider
      attribute="class"
      defaultTheme="light"
      storageKey="repoops-demo-theme"
      enableSystem
      disableTransitionOnChange
      enableColorScheme
    >
      <App />
    </ThemeProvider>
  </React.StrictMode>,
);
