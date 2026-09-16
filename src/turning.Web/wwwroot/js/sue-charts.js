// ============================================
// SUE — Gráficos con Canvas
// ============================================

window.sueDrawRadar = function (canvasId, data) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const dpr = window.devicePixelRatio || 1;
    const rect = canvas.getBoundingClientRect();
    const w = rect.width;
    const h = rect.height;

    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';

    const ctx = canvas.getContext('2d');
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, h);

    const cx = w / 2;
    const cy = h / 2;
    const radius = Math.min(w, h) * 0.34;
    const n = data.labels.length;

    // Anillos
    ctx.strokeStyle = '#D4B8A7';
    ctx.lineWidth = 1;
    for (let ring = 1; ring <= 4; ring++) {
        ctx.beginPath();
        for (let i = 0; i < n; i++) {
            const angle = -Math.PI / 2 + (i * 2 * Math.PI) / n;
            const r = (radius * ring) / 4;
            const x = cx + Math.cos(angle) * r;
            const y = cy + Math.sin(angle) * r;
            if (i) ctx.lineTo(x, y); else ctx.moveTo(x, y);
        }
        ctx.closePath();
        ctx.stroke();
    }

    // Ejes
    for (let i = 0; i < n; i++) {
        const angle = -Math.PI / 2 + (i * 2 * Math.PI) / n;
        ctx.beginPath();
        ctx.moveTo(cx, cy);
        ctx.lineTo(cx + Math.cos(angle) * radius, cy + Math.sin(angle) * radius);
        ctx.stroke();
    }

    // Área
    ctx.beginPath();
    data.values.forEach((v, i) => {
        const angle = -Math.PI / 2 + (i * 2 * Math.PI) / n;
        const x = cx + Math.cos(angle) * radius * v;
        const y = cy + Math.sin(angle) * radius * v;
        if (i) ctx.lineTo(x, y); else ctx.moveTo(x, y);
    });
    ctx.closePath();
    ctx.fillStyle = 'rgba(196,149,122,.28)';
    ctx.fill();
    ctx.strokeStyle = '#C4957A';
    ctx.lineWidth = 3;
    ctx.stroke();

    // Puntos
    data.values.forEach((v, i) => {
        const angle = -Math.PI / 2 + (i * 2 * Math.PI) / n;
        const x = cx + Math.cos(angle) * radius * v;
        const y = cy + Math.sin(angle) * radius * v;
        ctx.fillStyle = data.colors[i];
        ctx.beginPath();
        ctx.arc(x, y, 4, 0, Math.PI * 2);
        ctx.fill();
    });

    // Labels
    ctx.fillStyle = '#8B7A6B';
    ctx.font = 'bold 12px Manrope, sans-serif';
    data.labels.forEach((label, i) => {
        const angle = -Math.PI / 2 + (i * 2 * Math.PI) / n;
        const x = cx + Math.cos(angle) * (radius + 28);
        const y = cy + Math.sin(angle) * (radius + 28);
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(label, x, y);
    });
};
// ============================================
// Gráfico de barras
// ============================================
window.sueDrawBar = function (canvasId, data) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;

    const dpr = window.devicePixelRatio || 1;
    const rect = canvas.getBoundingClientRect();
    const w = rect.width;
    const h = rect.height;

    canvas.width = w * dpr;
    canvas.height = h * dpr;
    canvas.style.width = w + 'px';
    canvas.style.height = h + 'px';

    const ctx = canvas.getContext('2d');
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, w, h);

    const pad = { l: 40, r: 20, t: 20, b: 40 };
    const chartW = w - pad.l - pad.r;
    const chartH = h - pad.t - pad.b;
    const base = pad.t + chartH;

    // Eje X
    ctx.strokeStyle = '#D4B8A7';
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(pad.l, base);
    ctx.lineTo(w - pad.r, base);
    ctx.stroke();

    // Barras
    const n = data.values.length;
    const gap = chartW / n;
    const barW = gap * 0.55;

    data.values.forEach((v, i) => {
        const x = pad.l + gap * i + (gap - barW) / 2;
        const bh = (chartH * v) / 100;
        const y = base - bh;

        ctx.fillStyle = data.colors[i];
        ctx.beginPath();
        ctx.roundRect(x, y, barW, bh, 8);
        ctx.fill();

        // Label
        ctx.fillStyle = '#8B7A6B';
        ctx.font = '11px Manrope, sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText(data.labels[i], x + barW / 2, base + 20);
    });
};

// ============================================
// Descargar JSON
// ============================================
window.sueDownloadJson = function (filename, content) {
    const blob = new Blob([content], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
// ============================================
// Reset datos locales
// ============================================
window.sueResetLocalData = function () {
    try {
        localStorage.removeItem('sue_mockup_v1');
    } catch (e) {
        console.warn('No se pudo limpiar localStorage:', e);
    }
};