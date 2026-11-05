import React from "react";
import { Composition } from "remotion";
import { LONG_DURATION, PromoLong, Screenshot } from "./Compositions";
import { FPS } from "./timeline";

export const Root: React.FC = () => (
  <>
    <Composition id="Screenshot" component={Screenshot} durationInFrames={600} fps={FPS} width={1040} height={240} />
    <Composition id="PromoLong" component={PromoLong} durationInFrames={Math.round(LONG_DURATION * FPS)} fps={FPS} width={1920} height={1080} />
  </>
);
