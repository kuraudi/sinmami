"use client";

import { useRef, MouseEvent } from "react";

type Props = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  children: React.ReactNode;
};

export function ShimmerButton({ children, className = "", style, ...props }: Props) {
  const btnRef = useRef<HTMLButtonElement>(null);

  function handleMouseMove(e: MouseEvent<HTMLButtonElement>) {
    const btn = btnRef.current;
    if (!btn) return;
    const rect = btn.getBoundingClientRect();
    const x = ((e.clientX - rect.left) / rect.width) * 100;
    const y = ((e.clientY - rect.top) / rect.height) * 100;
    btn.style.setProperty("--mx", `${x}%`);
    btn.style.setProperty("--my", `${y}%`);
  }

  return (
    <button
      ref={btnRef}
      onMouseMove={handleMouseMove}
      className={`shimmer-btn ${className}`}
      style={style}
      {...props}
    >
      {children}
    </button>
  );
}
