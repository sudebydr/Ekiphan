"use client";

import { useEffect, useRef, useState } from "react";

type Metric = { value: number; suffix?: string; label: string };

const metrics: Metric[] = [
  { value: 25, suffix: "+", label: "YILLIK TECRÜBE" },
  { value: 150, suffix: "+", label: "GLOBAL MARKA" },
  { value: 2500, suffix: "+", label: "TAMAMLANAN PROJE" },
  { value: 45, label: "ÜLKEYE İHRACAT" }
];

export function MetricCounters() {
  const triggerRef = useRef<HTMLElement>(null);
  const [hasStarted, setHasStarted] = useState(false);
  const [values, setValues] = useState(() => metrics.map(() => 0));

  useEffect(() => {
    const element = triggerRef.current;
    if (!element) return;

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      setHasStarted(true);
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (!entry.isIntersecting) return;
        setHasStarted(true);
        observer.disconnect();
      },
      { threshold: 0.35 }
    );

    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    if (!hasStarted) return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
      setValues(metrics.map((metric) => metric.value));
      return;
    }

    const duration = 1350;
    const startTime = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const progress = Math.min((now - startTime) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setValues(metrics.map((metric) => Math.round(metric.value * eased)));
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [hasStarted]);

  return (
    <div style={{ display: "contents" }}>
      {metrics.map((metric, index) => (
        <article key={metric.label} ref={index === 0 ? triggerRef : undefined}>
          <div>
            <strong>{values[index]}{metric.suffix}</strong>
            <span>{metric.label}</span>
          </div>
        </article>
      ))}
    </div>
  );
}