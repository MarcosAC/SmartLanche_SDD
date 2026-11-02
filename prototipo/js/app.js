import { renderSidebar } from './components/sidebar.js';
import { renderHeader } from './components/header.js';

export function initLayout({ title }) {
  const app = document.getElementById('app');
  if (!app) return;
  app.innerHTML = `<div class="app-shell">
    ${renderSidebar(title)}
    <main class="main">
      ${renderHeader(title)}
      <section class="content" id="page-content"></section>
    </main>
  </div>`;
}
