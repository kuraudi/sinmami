"use client";

import { useEffect, useRef } from "react";

const BLOBS = [
  { color: "99,102,241",  x: 0.2, y: 0.2, speed: 0.0004, phase: 0 },
  { color: "139,92,246",  x: 0.8, y: 0.3, speed: 0.0003, phase: 2 },
  { color: "79,70,229",   x: 0.5, y: 0.7, speed: 0.0005, phase: 4 },
  { color: "124,58,237",  x: 0.1, y: 0.8, speed: 0.00035, phase: 1 },
];

export function MeshGradient({ className }: { className?: string }) {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    let animId: number;
    let t = 0;

    function resize() {
      if (!canvas) return;
      canvas.width = canvas.offsetWidth;
      canvas.height = canvas.offsetHeight;
    }

    function draw() {
      if (!canvas || !ctx) return;
      const w = canvas.width;
      const h = canvas.height;
      ctx.clearRect(0, 0, w, h);

      for (const blob of BLOBS) {
        const x = (blob.x + 0.18 * Math.sin(t * blob.speed * 1000 + blob.phase)) * w;
        const y = (blob.y + 0.15 * Math.cos(t * blob.speed * 800 + blob.phase)) * h;
        const r = 0.4 * Math.max(w, h);
        const grad = ctx.createRadialGradient(x, y, 0, x, y, r);
        grad.addColorStop(0, `rgba(${blob.color},0.28)`);
        grad.addColorStop(1, `rgba(${blob.color},0)`);
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, w, h);
      }

      t++;
      animId = requestAnimationFrame(draw);
    }

    resize();
    draw();

    const ro = new ResizeObserver(resize);
    ro.observe(canvas);

    return () => {
      cancelAnimationFrame(animId);
      ro.disconnect();
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      className={className}
      style={{ filter: "blur(60px)" }}
    />
  );
}
