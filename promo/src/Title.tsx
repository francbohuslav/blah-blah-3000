import React from "react";
import { AbsoluteFill, Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig } from "remotion";
import { C, FONT } from "./theme";
import { T } from "./timeline";
import { clamp } from "./util";

export const Title: React.FC = () => {
  const frame = useCurrentFrame();
  const { fps } = useVideoConfig();
  const f = frame / fps;
  const logo = spring({ frame, fps, config: { damping: 12 } });
  const text = interpolate(f, [0.5, 1.2], [0, 1], clamp);
  const tag = interpolate(f, [1.1, 1.8], [0, 1], clamp);
  const out = interpolate(f, [2.7, T.titleEnd], [1, 0], clamp);
  return (
    <AbsoluteFill style={{ alignItems: "center", justifyContent: "center", opacity: out, background: C.page }}>
      <Img src={staticFile("icon.svg")} style={{ width: 260, height: 260, transform: `scale(${logo})` }} />
      <div style={{ fontFamily: FONT, fontSize: 120, fontWeight: 700, color: C.text, opacity: text, marginTop: 24 }}>
        BlahBlah3000
      </div>
      <div style={{ fontFamily: FONT, fontSize: 40, color: C.muted, opacity: tag, marginTop: 10 }}>
        It listens to your blah blah and writes it down.
      </div>
    </AbsoluteFill>
  );
};
