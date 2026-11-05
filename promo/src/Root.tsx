import React from "react";
import { Composition } from "remotion";
import { LONG_DURATION, PromoLong, PromoShort, SHORT_DURATION } from "./Compositions";
import { FPS } from "./timeline";

export const Root: React.FC = () => (
  <>
    <Composition id="PromoShort" component={PromoShort} durationInFrames={Math.round(SHORT_DURATION * FPS)} fps={FPS} width={1920} height={1080} />
    <Composition id="PromoLong" component={PromoLong} durationInFrames={Math.round(LONG_DURATION * FPS)} fps={FPS} width={1920} height={1080} />
  </>
);
