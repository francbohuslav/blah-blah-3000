import React from "react";

// The application icon (assets/icon.svg) inline, so the background can turn red like the tray icon while recording.
export const AppIcon: React.FC<{ size: number; background?: string; stroke?: string; id?: string }> = ({
  size,
  background = "#202124",
  stroke = "#3c4043",
  id = "icon",
}) => (
  <svg width={size} height={size} viewBox="0 0 256 256">
    <defs>
      <clipPath id={`mouth-${id}`}>
        <path d="M 20 88 Q 128 112 236 88 Q 220 220 128 222 Q 36 220 20 88 Z" />
      </clipPath>
    </defs>
    <rect x="4" y="4" width="248" height="248" rx="56" fill={background} stroke={stroke} strokeWidth="3" />
    <path d="M 20 88 Q 128 112 236 88 Q 220 220 128 222 Q 36 220 20 88 Z" fill="#4a1313" />
    <g clipPath={`url(#mouth-${id})`}>
      <ellipse cx="128" cy="226" rx="52" ry="26" fill="#e8685f" />
      <path
        d="M 44 240 L 44 208 L 60.8 184 L 77.6 208 L 94.4 170 L 111.2 208 L 128.0 180 L 144.8 208 L 161.6 170 L 178.4 208 L 195.2 184 L 212.0 208 L 212 240 Z"
        fill="#ffffff"
        stroke="#3a1414"
        strokeWidth="3"
        strokeLinejoin="round"
      />
      <path
        d="M 20 70 L 20 112 L 35.4 138 L 50.9 112 L 66.3 158 L 81.7 112 L 97.1 146 L 112.6 112 L 128.0 166 L 143.4 112 L 158.9 146 L 174.3 112 L 189.7 158 L 205.1 112 L 220.6 138 L 236.0 112 L 236 70 Z"
        fill="#ffffff"
        stroke="#3a1414"
        strokeWidth="3"
        strokeLinejoin="round"
      />
    </g>
    <path
      d="M 20 88 Q 128 112 236 88 Q 220 220 128 222 Q 36 220 20 88 Z"
      fill="none"
      stroke="#f28b82"
      strokeWidth="11"
      strokeLinejoin="round"
    />
  </svg>
);
