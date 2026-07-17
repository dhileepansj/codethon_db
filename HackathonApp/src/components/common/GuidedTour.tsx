import { useState, useEffect, useCallback, useRef } from "react";
import { ChevronLeft, ChevronRight, X, Sparkles } from "lucide-react";

export interface TourStep {
  target: string;       // CSS selector for the element to highlight
  title: string;
  content: string;
  position?: "top" | "bottom" | "left" | "right";
}

interface GuidedTourProps {
  steps: TourStep[];
  onComplete: () => void;
  onSkip: () => void;
}

export default function GuidedTour({ steps, onComplete, onSkip }: GuidedTourProps) {
  const [currentStep, setCurrentStep] = useState(0);
  const [targetRect, setTargetRect] = useState<DOMRect | null>(null);
  const tooltipRef = useRef<HTMLDivElement>(null);

  const step = steps[currentStep];

  const updateTargetPosition = useCallback(() => {
    if (!step) return;
    const el = document.querySelector(step.target);
    if (el) {
      const rect = el.getBoundingClientRect();
      setTargetRect(rect);
      // Scroll element into view if needed
      el.scrollIntoView({ behavior: "smooth", block: "nearest" });
    } else {
      setTargetRect(null);
    }
  }, [step]);

  useEffect(() => {
    updateTargetPosition();
    // Reposition on resize/scroll
    window.addEventListener("resize", updateTargetPosition);
    window.addEventListener("scroll", updateTargetPosition, true);
    return () => {
      window.removeEventListener("resize", updateTargetPosition);
      window.removeEventListener("scroll", updateTargetPosition, true);
    };
  }, [updateTargetPosition, currentStep]);

  const handleNext = () => {
    if (currentStep < steps.length - 1) {
      setCurrentStep(currentStep + 1);
    } else {
      onComplete();
    }
  };

  const handlePrev = () => {
    if (currentStep > 0) {
      setCurrentStep(currentStep - 1);
    }
  };

  const getTooltipPosition = (): React.CSSProperties => {
    if (!targetRect) return { top: "50%", left: "50%", transform: "translate(-50%, -50%)" };

    const padding = 12;
    const tooltipWidth = 320; // w-80 = 20rem = 320px
    const pos = step.position || "bottom";

    let style: React.CSSProperties = {};

    switch (pos) {
      case "bottom":
        style = {
          top: targetRect.bottom + padding,
          left: targetRect.left + targetRect.width / 2,
          transform: "translateX(-50%)",
        };
        break;
      case "top":
        style = {
          bottom: window.innerHeight - targetRect.top + padding,
          left: targetRect.left + targetRect.width / 2,
          transform: "translateX(-50%)",
        };
        break;
      case "left":
        style = {
          top: targetRect.top + targetRect.height / 2,
          right: window.innerWidth - targetRect.left + padding,
          transform: "translateY(-50%)",
        };
        break;
      case "right":
        style = {
          top: targetRect.top + targetRect.height / 2,
          left: targetRect.right + padding,
          transform: "translateY(-50%)",
        };
        break;
    }

    // Clamp horizontal position to keep tooltip within viewport
    if (style.left != null && typeof style.left === "number") {
      const halfWidth = tooltipWidth / 2;
      if (style.left - halfWidth < 8) {
        style.left = 8;
        style.transform = style.transform?.replace("translateX(-50%)", "") || undefined;
      } else if (style.left + halfWidth > window.innerWidth - 8) {
        style.left = undefined;
        style.right = 8;
        style.transform = style.transform?.replace("translateX(-50%)", "") || undefined;
      }
    }

    return style;
  };

  return (
    <div className="fixed inset-0 z-[100]">
      {/* Overlay with cutout */}
      <svg className="absolute inset-0 w-full h-full" style={{ pointerEvents: "none" }}>
        <defs>
          <mask id="tour-mask">
            <rect x="0" y="0" width="100%" height="100%" fill="white" />
            {targetRect && (
              <rect
                x={targetRect.left - 4}
                y={targetRect.top - 4}
                width={targetRect.width + 8}
                height={targetRect.height + 8}
                rx="8"
                fill="black"
              />
            )}
          </mask>
        </defs>
        <rect
          x="0" y="0" width="100%" height="100%"
          fill="rgba(0,0,0,0.55)"
          mask="url(#tour-mask)"
          style={{ pointerEvents: "auto" }}
          onClick={(e) => e.stopPropagation()}
        />
      </svg>

      {/* Highlight ring around target */}
      {targetRect && (
        <div
          className="absolute border-2 border-teal-400 rounded-lg pointer-events-none animate-pulse"
          style={{
            top: targetRect.top - 4,
            left: targetRect.left - 4,
            width: targetRect.width + 8,
            height: targetRect.height + 8,
            boxShadow: "0 0 0 4px rgba(20, 184, 166, 0.2), 0 0 20px rgba(20, 184, 166, 0.3)",
          }}
        />
      )}

      {/* Tooltip */}
      <div
        ref={tooltipRef}
        className="absolute z-[101] w-80 bg-white dark:bg-gray-800 rounded-xl shadow-2xl border dark:border-gray-700 overflow-hidden"
        style={getTooltipPosition()}
      >
        {/* Header */}
        <div className="px-4 py-3 bg-gradient-to-r from-teal-500 to-cyan-500 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-white" />
            <span className="text-sm font-semibold text-white">{step.title}</span>
          </div>
          <button onClick={onSkip} className="p-1 hover:bg-white/20 rounded transition-colors">
            <X className="h-3.5 w-3.5 text-white" />
          </button>
        </div>

        {/* Content */}
        <div className="px-4 py-3">
          <p className="text-sm text-gray-600 dark:text-gray-300 leading-relaxed">{step.content}</p>
        </div>

        {/* Footer */}
        <div className="px-4 py-3 border-t dark:border-gray-700 flex items-center justify-between bg-gray-50 dark:bg-gray-800/50">
          <div className="flex items-center gap-1">
            {steps.map((_, idx) => (
              <div
                key={idx}
                className={`w-2 h-2 rounded-full transition-all ${
                  idx === currentStep
                    ? "bg-teal-500 w-4"
                    : idx < currentStep
                    ? "bg-teal-300 dark:bg-teal-700"
                    : "bg-gray-300 dark:bg-gray-600"
                }`}
              />
            ))}
          </div>
          <div className="flex items-center gap-2">
            {currentStep > 0 && (
              <button
                onClick={handlePrev}
                className="flex items-center gap-1 px-2.5 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors"
              >
                <ChevronLeft className="h-3.5 w-3.5" /> Back
              </button>
            )}
            <button
              onClick={handleNext}
              className="flex items-center gap-1 px-3 py-1.5 text-xs font-medium bg-teal-600 hover:bg-teal-700 text-white rounded-lg transition-colors"
            >
              {currentStep === steps.length - 1 ? "Finish" : "Next"}
              {currentStep < steps.length - 1 && <ChevronRight className="h-3.5 w-3.5" />}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
