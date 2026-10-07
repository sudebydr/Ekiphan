"use client";

import { useEffect, useRef, useState } from "react";

type Metric = { value: number; suffix?: string; label: string };

const metrics: Record<"tr" | "en", Metric[]> = {
  tr: [
  { value: 25, suffix: "+", label: "YILLIK TECRÜBE" },
  { value: 150, suffix: "+", label: "GLOBAL MARKA" },
  { value: 2500, suffix: "+", label: "TAMAMLANAN PROJE" },
  { value: 45, label: "ÜLKEYE İHRACAT" }
  ],
  en: [
    { value: 25, suffix: "+", label: "YEARS OF EXPERIENCE" },
    { value: 150, suffix: "+", label: "GLOBAL BRANDS" },
    { value: 2500, suffix: "+", label: "COMPLETED PROJECTS" },
    { value: 45, label: "COUNTRIES EXPORTED TO" }
  ]
};

export function MetricCounters({ locale = "tr" }: { locale?: "tr" | "en" }) {
  const localizedMetrics = metrics[locale];
  const triggerRef = useRef<HTMLElement>(null);
  const [hasStarted, setHasStarted] = useState(false);
  const [values, setValues] = useState(() => localizedMetrics.map(() => 0));

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
      setValues(localizedMetrics.map((metric) => metric.value));
      return;
    }

    const duration = 1350;
    const startTime = performance.now();
    let frame = 0;
    const tick = (now: number) => {
      const progress = Math.min((now - startTime) / duration, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      setValues(localizedMetrics.map((metric) => Math.round(metric.value * eased)));
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [hasStarted, locale]);

  return (
    <div style={{ display: "contents" }}>
      {localizedMetrics.map((metric, index) => (
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
