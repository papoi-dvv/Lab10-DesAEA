// Hotel.Web – fondo decorativo: luces doradas que suben despacio (como las
// lámparas del lobby). Se desactiva con la preferencia "reducir movimiento".

(function () {
    'use strict';

    const lienzo = document.getElementById('luces');
    if (!lienzo) return;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

    const ctx = lienzo.getContext('2d');
    let ancho = 0, alto = 0, luces = [], animacion = 0;

    const COLORES = ['233, 196, 106', '42, 157, 143', '255, 255, 255'];

    function crear() {
        return {
            x: Math.random() * ancho,
            y: alto + Math.random() * alto,
            r: 1 + Math.random() * 2.6,
            vel: 6 + Math.random() * 16,          // píxeles por segundo
            deriva: (Math.random() - 0.5) * 8,
            alfa: 0.12 + Math.random() * 0.35,
            color: COLORES[Math.floor(Math.random() * COLORES.length)]
        };
    }

    function medir() {
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        ancho = lienzo.clientWidth;
        alto = lienzo.clientHeight;
        lienzo.width = Math.floor(ancho * dpr);
        lienzo.height = Math.floor(alto * dpr);
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);

        const cantidad = Math.round(Math.min(70, (ancho * alto) / 24000));
        luces = Array.from({ length: cantidad }, crear);
        luces.forEach((l) => { l.y = Math.random() * alto; });
    }

    let anterior = performance.now();

    function dibujar(ahora) {
        const dt = Math.min((ahora - anterior) / 1000, 0.05);
        anterior = ahora;

        ctx.clearRect(0, 0, ancho, alto);

        for (const l of luces) {
            l.y -= l.vel * dt;
            l.x += l.deriva * dt;

            if (l.y + l.r < 0 || l.x < -20 || l.x > ancho + 20) {
                Object.assign(l, crear());
                l.y = alto + l.r;
            }

            const halo = ctx.createRadialGradient(l.x, l.y, 0, l.x, l.y, l.r * 5);
            halo.addColorStop(0, `rgba(${l.color}, ${l.alfa})`);
            halo.addColorStop(1, `rgba(${l.color}, 0)`);
            ctx.fillStyle = halo;
            ctx.beginPath();
            ctx.arc(l.x, l.y, l.r * 5, 0, Math.PI * 2);
            ctx.fill();
        }

        animacion = requestAnimationFrame(dibujar);
    }

    function iniciar() {
        medir();
        cancelAnimationFrame(animacion);
        anterior = performance.now();
        animacion = requestAnimationFrame(dibujar);
    }

    let temporizador = 0;
    window.addEventListener('resize', () => {
        clearTimeout(temporizador);
        temporizador = setTimeout(iniciar, 180);
    });

    // En segundo plano no se gasta CPU.
    document.addEventListener('visibilitychange', () => {
        if (document.hidden) cancelAnimationFrame(animacion);
        else { anterior = performance.now(); animacion = requestAnimationFrame(dibujar); }
    });

    iniciar();
})();
