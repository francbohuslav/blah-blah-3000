import React from "react";
import { Composition } from "remotion";
import { Promo } from "./Promo";
import { DURATION_S, FPS } from "./timeline";

export const Root: React.FC = () => (
  <Composition id="Promo" component={Promo} durationInFrames={DURATION_S * FPS} fps={FPS} width={1920} height={1080} />
);
