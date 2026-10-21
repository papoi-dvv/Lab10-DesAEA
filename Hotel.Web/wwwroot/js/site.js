// Hotel.Web – notificaciones (toasts) y contadores animados.

(function () {
    'use strict';

    const ICONOS = {
        creado: '➕',
        editado: '✏️',
        eliminado: '🗑️',
        reserva: '🛎️',
        cancelado: '✖️',
        advertencia: '⚠️',
        error: '⛔'
    };

    const reducirMovimiento = window.matchMedia('(prefers-reduced-motion: reduce)');

    function cerrar(el) {
        if (!el || el.classList.contains('saliendo')) return;
        el.classList.add('saliendo');
        el.addEventListener('animationend', () => el.remove(), { once: true });
        // Respaldo por si la animación no se dispara (pestaña en segundo plano).
        setTimeout(() => el.remove(), 600);
    }

    // Muestra una notificación. tipo: creado | editado | eliminado | reserva | cancelado | advertencia | error
    function toast(tipo, titulo, mensaje) {
        const contenedor = document.getElementById('toasts');
        if (!contenedor) return;
        if (!ICONOS[tipo]) tipo = 'creado';

        const el = document.createElement('div');
        el.className = 'toast-glass tipo-' + tipo;
        el.setAttribute('role', tipo === 'error' || tipo === 'advertencia' ? 'alert' : 'status');

        const icono = document.createElement('div');
        icono.className = 't-icono';
        icono.setAttribute('aria-hidden', 'true');
        icono.textContent = ICONOS[tipo];

        const cuerpo = document.createElement('div');
        const t = document.createElement('div');
        t.className = 't-titulo';
        t.textContent = titulo || '';
        cuerpo.appendChild(t);
        if (mensaje) {
            const m = document.createElement('div');
            m.className = 't-mensaje';
            m.textContent = mensaje;
            cuerpo.appendChild(m);
        }

        const x = document.createElement('button');
        x.type = 'button';
        x.className = 't-cerrar';
        x.setAttribute('aria-label', 'Cerrar notificación');
        x.textContent = '×';
        x.addEventListener('click', () => cerrar(el));

        el.append(icono, cuerpo, x);
        // Al terminar la barra de progreso (5,5 s) la notificación se cierra sola.
        el.addEventListener('animationend', (e) => {
            if (e.animationName === 'toast-barra') cerrar(el);
        });

        contenedor.appendChild(el);
        while (contenedor.children.length > 4) contenedor.firstElementChild.remove();
    }

    // Anima los números de los KPIs: <span data-contar="12" data-decimales="2">.
    function animarContadores(raiz) {
        (raiz || document).querySelectorAll('[data-contar]').forEach((el) => {
            const destino = parseFloat(el.dataset.contar);
            const decimales = parseInt(el.dataset.decimales || '0', 10);
            if (Number.isNaN(destino)) return;
            if (reducirMovimiento.matches || destino === 0) {
                el.textContent = destino.toFixed(decimales);
                return;
            }
            const duracion = 700;
            const inicio = performance.now();
            const paso = (ahora) => {
                const p = Math.min(1, (ahora - inicio) / duracion);
                const suave = 1 - Math.pow(1 - p, 3);
                el.textContent = (destino * suave).toFixed(decimales);
                if (p < 1) requestAnimationFrame(paso);
            };
            requestAnimationFrame(paso);
        });
    }

    window.toast = toast;
    window.animarContadores = animarContadores;

    document.addEventListener('DOMContentLoaded', () => {
        // Mensaje enviado por el servidor mediante TempData (Post/Redirect/Get).
        const inicial = document.getElementById('toast-inicial');
        if (inicial) {
            toast(inicial.dataset.tipo, inicial.dataset.titulo, inicial.dataset.mensaje);
            inicial.remove();
        }
        animarContadores();
    });
})();
